﻿using System;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using XamlCSS.Dom;
using XamlCSS.UWP.Dom;

namespace XamlCSS.UWP
{
    public class Css
    {
        public readonly static BaseCss<DependencyObject, DependencyObject, Style, DependencyProperty> instance =
            new BaseCss<DependencyObject, DependencyObject, Style, DependencyProperty>(
                new DependencyPropertyService(),
                new LogicalTreeNodeProvider(new DependencyPropertyService()),
                new StyleResourceService(),
                new StyleService(),
                DomElementBase<DependencyObject, DependencyProperty>.GetPrefix(typeof(Button)),
                new MarkupExtensionParser(),
                RunOnUIThread
                );

        public static void RunOnUIThread(Action action)
        {
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => action());
            }
            else
            {
                var localTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(0)
                };
                localTimer.Tick += (timer, e) =>
                {
                    (timer as DispatcherTimer).Stop();
                    action();
                };
                localTimer.Start();
            }
        }

        private static bool initialized = false;
        private static DispatcherTimer dispatcherTimer;
        static Css()
        {
            Initialize();
        }

        private static void LoadedDetectionHelper_SubTreeAdded(object sender, EventArgs e)
        {
            instance.UpdateElement(sender as DependencyObject);
        }
        private static void LoadedDetectionHelper_SubTreeRemoved(object sender, EventArgs e)
        {
            instance.UnapplyMatchingStyles(sender as DependencyObject);
        }
        
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            LoadedDetectionHelper.Initialize();

            LoadedDetectionHelper.SubTreeAdded += LoadedDetectionHelper_SubTreeAdded;
            LoadedDetectionHelper.SubTreeRemoved += LoadedDetectionHelper_SubTreeRemoved;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(16);
            dispatcherTimer.Tick += (s, e) =>
            {
                Initialize();
                instance.ExecuteApplyStyles();
            };
            dispatcherTimer.Start();

            initialized = true;
        }

        #region dependency properties

        public static readonly DependencyProperty MatchingStylesProperty =
            DependencyProperty.RegisterAttached(
                "MatchingStyles",
                typeof(string[]),
                typeof(Css),
                new PropertyMetadata(null));
        public static string[] GetMatchingStyles(DependencyObject obj)
        {
            return obj.ReadLocalValue(MatchingStylesProperty) as string[];
        }
        public static void SetMatchingStyles(DependencyObject obj, string[] value)
        {
            obj.SetValue(MatchingStylesProperty, value);
        }

        public static readonly DependencyProperty AppliedMatchingStylesProperty =
            DependencyProperty.RegisterAttached(
                "AppliedMatchingStyles",
                typeof(string[]),
                typeof(Css),
                new PropertyMetadata(null));
        public static string[] GetAppliedMatchingStyles(DependencyObject obj)
        {
            return obj.ReadLocalValue(AppliedMatchingStylesProperty) as string[];
        }
        public static void SetAppliedMatchingStyles(DependencyObject obj, string[] value)
        {
            obj.SetValue(AppliedMatchingStylesProperty, value);
        }

        public static readonly DependencyProperty InitialStyleProperty =
            DependencyProperty.RegisterAttached("InitialStyle", typeof(Style),
            typeof(Css), new PropertyMetadata(null));
        public static Style GetInitialStyle(DependencyObject obj)
        {
            return obj.ReadLocalValue(InitialStyleProperty) as Style;
        }
        public static void SetInitialStyle(DependencyObject obj, Style value)
        {
            obj.SetValue(InitialStyleProperty, value ?? DependencyProperty.UnsetValue);
        }

        public static readonly DependencyProperty HadStyleProperty =
            DependencyProperty.RegisterAttached("HadStyle", typeof(bool?),
            typeof(Css), new PropertyMetadata(null));
        public static bool? GetHadStyle(DependencyObject obj)
        {
            return obj.ReadLocalValue(HadStyleProperty) as bool?;
        }
        public static void SetHadStyle(DependencyObject obj, bool? value)
        {
            obj.SetValue(HadStyleProperty, value ?? DependencyProperty.UnsetValue);
        }

        public static readonly DependencyProperty StyleProperty =
            DependencyProperty.RegisterAttached("Style", typeof(StyleDeclarationBlock),
            typeof(Css), new PropertyMetadata(null, StylePropertyAttached));
        public static StyleDeclarationBlock GetStyle(DependencyObject obj)
        {
            return obj.ReadLocalValue(StyleProperty) as StyleDeclarationBlock;
        }
        public static void SetStyle(DependencyObject obj, StyleDeclarationBlock value)
        {
            obj.SetValue(StyleProperty, value ?? DependencyProperty.UnsetValue);
        }

        public static readonly DependencyProperty StyleSheetProperty =
            DependencyProperty.RegisterAttached("StyleSheet", typeof(StyleSheet),
            typeof(Css), new PropertyMetadata(null, StyleSheetPropertyChanged));
        public static StyleSheet GetStyleSheet(DependencyObject obj)
        {
            var read = obj.ReadLocalValue(StyleSheetProperty);
            if (read is BindingExpression)
                read = obj.GetValue(StyleSheetProperty);
            return read as StyleSheet;
        }
        public static void SetStyleSheet(DependencyObject obj, StyleSheet value)
        {
            obj.SetValue(StyleSheetProperty, value ?? DependencyProperty.UnsetValue);
        }

        public static readonly DependencyProperty ClassProperty =
            DependencyProperty.RegisterAttached("Class", typeof(string),
            typeof(Css), new PropertyMetadata(null, ClassPropertyAttached));
        private static void ClassPropertyAttached(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            var domElement = GetDomElement(element) as DomElementBase<DependencyObject, DependencyProperty>;
            domElement?.ResetClassList();

            Css.instance.UpdateElement(element);
        }

        public static string GetClass(DependencyObject obj)
        {
            return obj.ReadLocalValue(ClassProperty) as string;
        }
        public static void SetClass(DependencyObject obj, string value)
        {
            obj.SetValue(ClassProperty, value ?? DependencyProperty.UnsetValue);
        }

        public static readonly DependencyProperty HandledCssProperty =
            DependencyProperty.RegisterAttached("HandledCss", typeof(bool),
            typeof(Css), new PropertyMetadata(null, null));

        public static bool GetHandledCss(DependencyObject obj)
        {
            var res = obj.ReadLocalValue(HandledCssProperty);
            if (res == DependencyProperty.UnsetValue)
                return false;
            return (bool)res;
        }
        public static void SetHandledCss(DependencyObject obj, bool value)
        {
            obj.SetValue(HandledCssProperty, value == true ? true : DependencyProperty.UnsetValue);
        }

        public static readonly DependencyProperty DomElementProperty =
            DependencyProperty.RegisterAttached("DomElement", typeof(bool),
            typeof(Css), new PropertyMetadata(null, null));

        public static IDomElement<DependencyObject> GetDomElement(DependencyObject obj)
        {
            var res = obj.ReadLocalValue(DomElementProperty);
            if (res == DependencyProperty.UnsetValue)
                return null;
            return res as IDomElement<DependencyObject>;
        }
        public static void SetDomElement(DependencyObject obj, IDomElement<DependencyObject> value)
        {
            obj.SetValue(DomElementProperty, value ?? DependencyProperty.UnsetValue);
        }

        #endregion

        #region attached behaviours

        private static void StyleSheetPropertyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            // Debug.WriteLine($"StyleSheetPropertyChanged: {e.NewValue.ToString()}");
            
            if (e.OldValue != null)
            {
                var oldStyleSheet = e.OldValue as StyleSheet;
                oldStyleSheet.PropertyChanged -= NewStyleSheet_PropertyChanged;
                oldStyleSheet.AttachedTo = null;

                instance.RemoveStyleResources(element, oldStyleSheet);
            }

            var newStyleSheet = (StyleSheet)e.NewValue;

            if (newStyleSheet == null)
            {
                return;
            }

            newStyleSheet.PropertyChanged += NewStyleSheet_PropertyChanged;
            newStyleSheet.AttachedTo = element;

            instance.EnqueueRenderStyleSheet(element, newStyleSheet, null);
        }

        private static void NewStyleSheet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var styleSheet = sender as StyleSheet;
            var attachedTo = styleSheet.AttachedTo as FrameworkElement;

            instance.EnqueueRemoveStyleSheet(attachedTo, styleSheet, null);
            instance.EnqueueRenderStyleSheet(attachedTo, styleSheet, null);
        }

        private static void StylePropertyAttached(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            instance.UpdateElement(d as FrameworkElement);
        }

        #endregion
    }
}
