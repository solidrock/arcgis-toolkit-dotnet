﻿// /*******************************************************************************
//  * Copyright 2012-2018 Esri
//  *
//  *  Licensed under the Apache License, Version 2.0 (the "License");
//  *  you may not use this file except in compliance with the License.
//  *  You may obtain a copy of the License at
//  *
//  *  http://www.apache.org/licenses/LICENSE-2.0
//  *
//  *   Unless required by applicable law or agreed to in writing, software
//  *   distributed under the License is distributed on an "AS IS" BASIS,
//  *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  *   See the License for the specific language governing permissions and
//  *   limitations under the License.
//  ******************************************************************************/

using System.Collections.ObjectModel;
using System.Threading;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Toolkit.Internal;

namespace Esri.ArcGISRuntime.Toolkit.UI.Controls
{
    [Register("Esri.ArcGISRuntime.Toolkit.UI.Controls.LayerLegend")]
    public partial class LayerLegend
    {
        private ListView _listView;
        private SynchronizationContext _syncContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        public LayerLegend(Context context)
            : base(context)
        {
            Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayerLegend"/> class.
        /// </summary>
        /// <param name="context">The Context the view is running in, through which it can access resources, themes, etc</param>
        /// <param name="attr">The attributes of the AXML element declaring the view</param>
        public LayerLegend(Context context, IAttributeSet attr)
            : base(context, attr)
        {
            Initialize();
        }

        private void Initialize()
        {
            _syncContext = SynchronizationContext.Current ?? new SynchronizationContext();

            _listView = new ListView(Context)
            {
                ClipToOutline = true,
                Clickable = false,
                ChoiceMode = ChoiceMode.None,
                LayoutParameters = new LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent),
                ScrollingCacheEnabled = false,
                PersistentDrawingCache = PersistentDrawingCaches.NoCache,
            };

            AddView(_listView);
        }

        private void Refresh()
        {
            if (_listView == null)
            {
                return;
            }

            if (LayerContent == null)
            {
                _listView.Adapter = null;
                return;
            }

            if (LayerContent is ILoadable)
            {
                if ((LayerContent as ILoadable).LoadStatus != LoadStatus.Loaded)
                {
                    (LayerContent as ILoadable).Loaded += Layer_Loaded;
                    (LayerContent as ILoadable).LoadAsync();
                    return;
                }
            }

            var items = new ObservableCollection<LegendInfo>();
            LoadRecursive(items, LayerContent, IncludeSublayers);
            _listView.Adapter = new LayerLegendAdapter(Context, items);
            _listView.SetHeightBasedOnChildren();
        }

        private void Layer_Loaded(object sender, System.EventArgs e)
        {
            (sender as ILoadable).Loaded -= Layer_Loaded;
            _syncContext?.Post(_ => Refresh(), null);
        }

        /// <inheritdoc />
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            // Initialize dimensions of root layout
            MeasureChild(_listView, widthMeasureSpec, MeasureSpec.MakeMeasureSpec(MeasureSpec.GetSize(heightMeasureSpec), MeasureSpecMode.AtMost));

            // Calculate the ideal width and height for the view
            var desiredWidth = PaddingLeft + PaddingRight + _listView.MeasuredWidth;
            var desiredHeight = PaddingTop + PaddingBottom + _listView.MeasuredHeight;

            // Get the width and height of the view given any width and height constraints indicated by the width and height spec values
            var width = ResolveSize(desiredWidth, widthMeasureSpec);
            var height = ResolveSize(desiredHeight, heightMeasureSpec);
            SetMeasuredDimension(width, height);
        }

        /// <inheritdoc />
        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            // Forward layout call to the root layout
            _listView.Layout(PaddingLeft, PaddingTop, _listView.MeasuredWidth + PaddingLeft, _listView.MeasuredHeight + PaddingBottom);
        }
    }
}