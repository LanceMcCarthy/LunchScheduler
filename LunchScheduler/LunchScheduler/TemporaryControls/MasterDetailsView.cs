﻿using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Microsoft.Toolkit.Uwp.UI;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace LunchScheduler.TemporaryControls
{
    /// <summary>
    /// Panel that allows for a Master/Details pattern.
    /// </summary>
    [TemplatePart(Name = PartDetailsPresenter, Type = typeof(ContentPresenter))]
    [TemplatePart(Name = PartDetailsPanel, Type = typeof(FrameworkElement))]
    [TemplateVisualState(Name = NoSelectionNarrowState, GroupName = SelectionStates)]
    [TemplateVisualState(Name = NoSelectionWideState, GroupName = SelectionStates)]
    [TemplateVisualState(Name = NarrowState, GroupName = WidthStates)]
    [TemplateVisualState(Name = WideState, GroupName = WidthStates)]
    public partial class MasterDetailsView : ItemsControl
    {
        private const string PartDetailsPresenter = "DetailsPresenter";
        private const string PartDetailsPanel = "DetailsPanel";
        private const string PartHeaderContentPresenter = "HeaderContentPresenter";
        private const string NarrowState = "NarrowState";
        private const string WideState = "WideState";
        private const string WidthStates = "WidthStates";
        private const string SelectionStates = "SelectionStates";
        private const string HasSelectionState = "HasSelection";
        private const string NoSelectionNarrowState = "NoSelectionNarrow";
        private const string NoSelectionWideState = "NoSelectionWide";

        private ContentPresenter _detailsPresenter;
        private VisualStateGroup _stateGroup;
        private VisualState _narrowState;
        private Frame _frame;
        //private Visual _root; // TODO Shawn  - remove
        //private Compositor _compositor; // TODO Shawn  - remove
        //private Visual _detailsVisual; // TODO Shawn  - remove

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterDetailsView"/> class.
        /// </summary>
        public MasterDetailsView()
        {
            DefaultStyleKey = typeof(MasterDetailsView);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// Invoked whenever application code or internal processes (such as a rebuilding layout pass) call
        /// ApplyTemplate. In simplest terms, this means the method is called just before a UI element displays
        /// in your app. Override this method to influence the default post-template logic of a class.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _detailsPresenter = (ContentPresenter)GetTemplateChild(PartDetailsPresenter);
            
            //if (!DesignMode.DesignModeEnabled)
            //{
            //    var detailsPanel = (FrameworkElement)GetTemplateChild(PartDetailsPanel); // TODO Shawn  - remove
            //    _root = ElementCompositionPreview.GetElementVisual(detailsPanel); // TODO Shawn  - remove
            //    _compositor = _root.Compositor; // TODO Shawn  - remove
            //    _detailsVisual = ElementCompositionPreview.GetElementVisual(_detailsPresenter); // TODO Shawn  - remove
            //}
            
            SetMasterHeaderVisibility();
        }

        /// <summary>
        /// Fired when the SelectedItem changes.
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        /// <remarks>
        /// Sets up animations for the DetailsPresenter for animating in/out.
        /// </remarks>
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (MasterDetailsView)d;
            
            string noSelectionState = view._stateGroup.CurrentState == view._narrowState
                ? NoSelectionNarrowState
                : NoSelectionWideState;

            VisualStateManager.GoToState(view, view.SelectedItem == null ? noSelectionState : HasSelectionState, true);

            view.OnSelectionChanged(new SelectionChangedEventArgs(new List<object> { e.OldValue }, new List<object> { e.NewValue }));

            // If there is no selection, do not remove the DetailsPresenter content but let it animate out.
            if (view.SelectedItem != null)
            {
                view._detailsPresenter.Content = view.MapDetails == null
                    ? view.SelectedItem
                    : view.MapDetails(view.SelectedItem);
            }

            view.SetBackButtonVisibility(view._stateGroup.CurrentState);
        }

        /// <summary>
        /// Fired when the <see cref="MasterDetailsView.MasterHeader"/> is changed.
        /// </summary>
        /// <param name="d">The sender</param>
        /// <param name="e">The event args</param>
        private static void OnMasterHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (MasterDetailsView)d;
            view.SetMasterHeaderVisibility();
        }

        // Have to wait to get the VisualStateGroup until the control has Loaded
        // If we try to get the VisualStateGroup in the OnApplyTemplate the
        // CurrentStateChanged event does not fire properly
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // TODO Shawn  - wrap
            if (!DesignMode.DesignModeEnabled)
            {
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
            }

            if (_stateGroup != null)
            {
                _stateGroup.CurrentStateChanged -= OnVisualStateChanged;
            }

            _stateGroup = (VisualStateGroup)GetTemplateChild(WidthStates);
            _stateGroup.CurrentStateChanged += OnVisualStateChanged;

            _narrowState = GetTemplateChild(NarrowState) as VisualState;

            string noSelectionState = _stateGroup.CurrentState == _narrowState
                ? NoSelectionNarrowState
                : NoSelectionWideState;

            VisualStateManager.GoToState(this, this.SelectedItem == null ? noSelectionState : HasSelectionState, true);

            UpdateViewState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // TODO Shawn  - wrap
            if (!DesignMode.DesignModeEnabled)
            {
                SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;
            } 
        }

        /// <summary>
        /// Fires when the addaptive trigger changes state.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event args</param>
        /// <remarks>
        /// Handles showing/hiding the back button when the state changes
        /// </remarks>
        private void OnVisualStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            SetBackButtonVisibility(e.NewState);

            // When adaptive trigger changes state, switch between NoSelectionWide and NoSelectionNarrow.
            string noSelectionState = e.NewState == _narrowState
                ? NoSelectionNarrowState
                : NoSelectionWideState;

            VisualStateManager.GoToState(this, this.SelectedItem == null ? noSelectionState : HasSelectionState, false);
        }

        /// <summary>
        /// Closes the details pane if we are in narrow state
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The event args</param>
        private void OnBackRequested(object sender, BackRequestedEventArgs args)
        {
            if (ViewState == MasterDetailsViewState.Details)
            {
                SelectedItem = null;
                args.Handled = true;
            }
        }

        private void SetMasterHeaderVisibility()
        {
            var headerPresenter = GetTemplateChild(PartHeaderContentPresenter) as FrameworkElement;
            if (headerPresenter != null)
            {
                headerPresenter.Visibility = MasterHeader != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Sets the back button visibility based on the current visual state and selected item
        /// </summary>
        private void SetBackButtonVisibility(VisualState currentState)
        {
            UpdateViewState();

            if (ViewState == MasterDetailsViewState.Details)
            {
                // TODO Shawn  - wrap
                if (!DesignMode.DesignModeEnabled)
                {
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    AppViewBackButtonVisibility.Visible;
                }
            }
            else
            {
                // Make sure we show the back button if the stack can navigate back
                var frame = GetFrame();

                // TODO Shawn  - wrap
                if (!DesignMode.DesignModeEnabled)
                {
                    SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    ((frame != null) && frame.CanGoBack)
                        ? AppViewBackButtonVisibility.Visible
                        : AppViewBackButtonVisibility.Collapsed;
                }
            }
        }

        private Frame GetFrame()
        {
            return _frame ?? (_frame = this.FindVisualAscendant<Frame>());
        }

        private void UpdateViewState()
        {
            var before = ViewState;

            if (_stateGroup.CurrentState == _narrowState || _stateGroup.CurrentState == null)
            {
                ViewState = SelectedItem == null ? MasterDetailsViewState.Master : MasterDetailsViewState.Details;
            }
            else
            {
                ViewState = MasterDetailsViewState.Both;
            }

            var after = ViewState;

            if (before != after)
            {
                ViewStateChanged?.Invoke(this, after);
            }
        }
    }
}
