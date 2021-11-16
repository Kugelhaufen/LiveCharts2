﻿// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;

namespace LiveChartsCore
{
    /// <summary>
    /// Defines the data to plot as a polar line.
    /// </summary>
    /// <typeparam name="TModel">The type of the model to plot.</typeparam>
    /// <typeparam name="TVisual">The type of the visual point.</typeparam>
    /// <typeparam name="TLabel">The type of the data label.</typeparam>
    /// <typeparam name="TDrawingContext">The type of the drawing context.</typeparam>
    /// <typeparam name="TPathGeometry">The type of the path geometry.</typeparam>
    /// <typeparam name="TLineSegment">The type of the line segment.</typeparam>
    /// <typeparam name="TBezierSegment">The type of the bezier segmen.</typeparam>
    /// <typeparam name="TMoveToCommand">The type of the move command.</typeparam>
    /// <typeparam name="TPathArgs">The type of the path arguments.</typeparam>
    public class PolarLineSeries<TModel, TVisual, TLabel, TDrawingContext, TPathGeometry, TLineSegment, TBezierSegment, TMoveToCommand, TPathArgs>
        : ChartSeries<TModel, LineBezierVisualPoint<TDrawingContext, TVisual, TBezierSegment, TPathArgs>, TLabel, TDrawingContext>, IPolarLineSeries<TDrawingContext>, IPolarSeries<TDrawingContext>
            where TPathGeometry : IPathGeometry<TDrawingContext, TPathArgs>, new()
            where TLineSegment : ILinePathSegment<TPathArgs>, new()
            where TBezierSegment : IBezierSegment<TPathArgs>, new()
            where TMoveToCommand : IMoveToPathCommand<TPathArgs>, new()
            where TVisual : class, ISizedVisualChartPoint<TDrawingContext>, new()
            where TLabel : class, ILabelGeometry<TDrawingContext>, new()
            where TDrawingContext : DrawingContext
    {
        private readonly Dictionary<object, List<AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs>>> _fillPathHelperDictionary = new();
        private readonly Dictionary<object, List<AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs>>> _strokePathHelperDictionary = new();
        private float _lineSmoothness = 0.65f;
        private float _geometrySize = 14f;
        private bool _enableNullSplitting = true;
        private IPaint<TDrawingContext>? _geometryFill;
        private IPaint<TDrawingContext>? _geometryStroke;
        private IPaint<TDrawingContext>? _stroke = null;
        private IPaint<TDrawingContext>? _fill = null;
        private int _scalesAngleAt;
        private int _scalesRadiusAt;
        private bool _isClosed = true;
        private PolarLabelsPosition _labelsPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolarLineSeries{TModel, TVisual, TLabel, TDrawingContext, TPathGeometry, TLineSegment, TBezierSegment, TMoveToCommand, TPathArgs}"/> class.
        /// </summary>
        /// <param name="isStacked">if set to <c>true</c> [is stacked].</param>
        public PolarLineSeries(bool isStacked = false)
            : base(
                  SeriesProperties.Polar | SeriesProperties.PrimaryAxisVerticalOrientation |
                  (isStacked ? SeriesProperties.Stacked : 0) | SeriesProperties.Sketch | SeriesProperties.PrefersXStrategyTooltips)
        {
            DataPadding = new LvcPoint(1f, 1.5f);
        }

        /// <summary>
        /// Gets or sets the stroke.
        /// </summary>
        /// <value>
        /// The stroke.
        /// </value>
        public IPaint<TDrawingContext>? Stroke
        {
            get => _stroke;
            set => SetPaintProperty(ref _stroke, value, true);
        }

        /// <summary>
        /// Gets or sets the fill.
        /// </summary>
        /// <value>
        /// The fill.
        /// </value>
        public IPaint<TDrawingContext>? Fill
        {
            get => _fill;
            set => SetPaintProperty(ref _fill, value);
        }

        /// <inheritdoc cref="ILineSeries{TDrawingContext}.GeometrySize"/>
        public double GeometrySize { get => _geometrySize; set { _geometrySize = (float)value; OnPropertyChanged(); } }

        /// <inheritdoc cref="IPolarSeries{TDrawingContext}.ScalesAngleAt"/>
        public int ScalesAngleAt { get => _scalesAngleAt; set { _scalesAngleAt = value; OnPropertyChanged(); } }

        /// <inheritdoc cref="IPolarSeries{TDrawingContext}.ScalesRadiusAt"/>
        public int ScalesRadiusAt { get => _scalesRadiusAt; set { _scalesRadiusAt = value; OnPropertyChanged(); } }

        /// <inheritdoc cref="ILineSeries{TDrawingContext}.LineSmoothness"/>
        public double LineSmoothness
        {
            get => _lineSmoothness;
            set
            {
                var v = value;
                if (value > 1) v = 1;
                if (value < 0) v = 0;
                _lineSmoothness = (float)v;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc cref="ILineSeries{TDrawingContext}.EnableNullSplitting"/>
        public bool EnableNullSplitting { get => _enableNullSplitting; set { _enableNullSplitting = value; OnPropertyChanged(); } }

        /// <inheritdoc cref="ILineSeries{TDrawingContext}.GeometryFill"/>
        public IPaint<TDrawingContext>? GeometryFill
        {
            get => _geometryFill;
            set => SetPaintProperty(ref _geometryFill, value);
        }

        /// <inheritdoc cref="ILineSeries{TDrawingContext}.GeometryStroke"/>
        public IPaint<TDrawingContext>? GeometryStroke
        {
            get => _geometryStroke;
            set => SetPaintProperty(ref _geometryStroke, value, true);
        }

        /// <inheritdoc cref="IPolarLineSeries{TDrawingContext}.IsClosed"/>
        public bool IsClosed { get => _isClosed; set { _isClosed = value; OnPropertyChanged(); } }

        /// <inheritdoc cref="IPolarSeries{TDrawingContext}.DataLabelsPosition"/>
        public PolarLabelsPosition DataLabelsPosition { get => _labelsPosition; set { _labelsPosition = value; OnPropertyChanged(); } }

        /// <inheritdoc cref="ChartElement{TDrawingContext}.Measure(Chart{TDrawingContext})"/>
        public override void Measure(Chart<TDrawingContext> chart)
        {
            if (GetCustomMeasureHandler() is not null)
            {
                GetCustomMeasureHandler()!(chart);
                return;
            }

            var polarChart = (PolarChart<TDrawingContext>)chart;
            var angleAxis = polarChart.AngleAxes[ScalesAngleAt];
            var radiusAxis = polarChart.RadiusAxes[ScalesRadiusAt];

            var drawLocation = polarChart.DrawMarginLocation;
            var drawMarginSize = polarChart.DrawMarginSize;

            var scaler = new PolarScaler(
                drawLocation, drawMarginSize, angleAxis, radiusAxis,
                polarChart.InnerRadius, polarChart.InitialRotation, polarChart.TotalAnge);

            var gs = _geometrySize;
            var hgs = gs / 2f;
            var sw = Stroke?.StrokeThickness ?? 0;

            var chartAnimation = new Animation(EasingFunction ?? polarChart.EasingFunction, AnimationsSpeed ?? polarChart.AnimationsSpeed);

            var fetched = Fetch(polarChart);
            if (fetched is not ChartPoint[] points) points = fetched.ToArray();

            var segments = _enableNullSplitting
                ? SplitEachNull(points, scaler)
                : new ChartPoint[][] { points };

            var stacker = (SeriesProperties & SeriesProperties.Stacked) == SeriesProperties.Stacked
                ? polarChart.SeriesContext.GetStackPosition(this, GetStackGroup())
                : null;

            var actualZIndex = ZIndex == 0 ? ((ISeries)this).SeriesId : ZIndex;

            if (stacker is not null)
            {
                // easy workaround to set an automatic and valid z-index for stacked area series
                // the problem of this solution is that the user needs to set z-indexes above 1000
                // if the user needs to add more series to the chart.
                actualZIndex = 1000 - stacker.Position;
                if (Fill is not null) Fill.ZIndex = actualZIndex;
                if (Stroke is not null) Stroke.ZIndex = actualZIndex;
            }

            var dls = unchecked((float)DataLabelsSize);

            var segmentI = 0;
            var toDeletePoints = new HashSet<ChartPoint>(everFetched);

            if (!_strokePathHelperDictionary.TryGetValue(chart.Canvas.Sync, out var strokePathHelperContainer))
            {
                strokePathHelperContainer = new List<AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs>>();
                _strokePathHelperDictionary[chart.Canvas.Sync] = strokePathHelperContainer;
            }

            if (!_fillPathHelperDictionary.TryGetValue(chart.Canvas.Sync, out var fillPathHelperContainer))
            {
                fillPathHelperContainer = new List<AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs>>();
                _fillPathHelperDictionary[chart.Canvas.Sync] = fillPathHelperContainer;
            }

            foreach (var item in strokePathHelperContainer) item.Path.ClearCommands();
            foreach (var item in fillPathHelperContainer) item.Path.ClearCommands();

            var r = (float)DataLabelsRotation;
            var isTangent = false;
            var isCotangent = false;

            if (((int)r & LiveCharts.TangentAngle) != 0)
            {
                r -= LiveCharts.TangentAngle;
                isTangent = true;
            }

            if (((int)r & LiveCharts.CotangentAngle) != 0)
            {
                r -= LiveCharts.CotangentAngle;
                isCotangent = true;
            }

            foreach (var segment in segments)
            {
                var wasFillInitialized = false;
                var wasStrokeInitialized = false;

                AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs> fillPathHelper;
                AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs> strokePathHelper;

                if (segmentI >= fillPathHelperContainer.Count)
                {
                    fillPathHelper = new AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs>();
                    strokePathHelper = new AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs>();
                    fillPathHelperContainer.Add(fillPathHelper);
                    strokePathHelperContainer.Add(strokePathHelper);
                }
                else
                {
                    fillPathHelper = fillPathHelperContainer[segmentI];
                    strokePathHelper = strokePathHelperContainer[segmentI];
                }

                if (Fill is not null)
                {
                    wasFillInitialized = fillPathHelper.Initialize(SetDefaultPathTransitions, chartAnimation);
                    Fill.AddGeometryToPaintTask(polarChart.Canvas, fillPathHelper.Path);
                    polarChart.Canvas.AddDrawableTask(Fill);
                    Fill.ZIndex = actualZIndex + 0.1;
                    Fill.SetClipRectangle(polarChart.Canvas, new LvcRectangle(drawLocation, drawMarginSize));
                }
                if (Stroke is not null)
                {
                    wasStrokeInitialized = strokePathHelper.Initialize(SetDefaultPathTransitions, chartAnimation);
                    Stroke.AddGeometryToPaintTask(polarChart.Canvas, strokePathHelper.Path);
                    polarChart.Canvas.AddDrawableTask(Stroke);
                    Stroke.ZIndex = actualZIndex + 0.2;
                    Stroke.SetClipRectangle(polarChart.Canvas, new LvcRectangle(drawLocation, drawMarginSize));
                    strokePathHelper.Path.IsClosed = IsClosed;
                }

                foreach (var data in GetSpline(segment, scaler, stacker))
                {
                    var s = 0d;
                    if (stacker is not null)
                    {
                        s = stacker.GetStack(data.TargetPoint).Start;
                    }

                    var cp = scaler.ToPixels(data.TargetPoint.SecondaryValue, data.TargetPoint.PrimaryValue + s);

                    var x = cp.X;
                    var y = cp.Y;

                    var visual = (LineBezierVisualPoint<TDrawingContext, TVisual, TBezierSegment, TPathArgs>?)data.TargetPoint.Context.Visual;

                    if (visual is null)
                    {
                        var v = new LineBezierVisualPoint<TDrawingContext, TVisual, TBezierSegment, TPathArgs>();

                        visual = v;

                        var x0b = scaler.CenterX - hgs;
                        var x1b = scaler.CenterX - hgs;
                        var x2b = scaler.CenterX - hgs;
                        var y0b = scaler.CenterY - hgs;
                        var y1b = scaler.CenterY - hgs;
                        var y2b = scaler.CenterY - hgs;

                        v.Geometry.X = scaler.CenterX;
                        v.Geometry.Y = scaler.CenterY;
                        v.Geometry.Width = gs;
                        v.Geometry.Height = gs;

                        v.Bezier.X0 = (float)x0b;
                        v.Bezier.Y0 = y0b;
                        v.Bezier.X1 = (float)x1b;
                        v.Bezier.Y1 = y1b;
                        v.Bezier.X2 = (float)x2b;
                        v.Bezier.Y2 = y2b;

                        data.TargetPoint.Context.Visual = v;
                        OnPointCreated(data.TargetPoint);
                    }

                    _ = everFetched.Add(data.TargetPoint);

                    if (GeometryFill is not null) GeometryFill.AddGeometryToPaintTask(polarChart.Canvas, visual.Geometry);
                    if (GeometryStroke is not null) GeometryStroke.AddGeometryToPaintTask(polarChart.Canvas, visual.Geometry);

                    visual.Bezier.X0 = (float)data.X0;
                    visual.Bezier.Y0 = (float)data.Y0;
                    visual.Bezier.X1 = (float)data.X1;
                    visual.Bezier.Y1 = (float)data.Y1;
                    visual.Bezier.X2 = (float)data.X2;
                    visual.Bezier.Y2 = (float)data.Y2;

                    if (Fill is not null)
                    {
                        if (data.IsFirst)
                        {
                            if (wasFillInitialized)
                            {
                                fillPathHelper.StartPoint.X = scaler.CenterX;
                                fillPathHelper.StartPoint.Y = scaler.CenterY;

                                fillPathHelper.StartSegment.X = scaler.CenterX;
                                fillPathHelper.StartSegment.Y = scaler.CenterY;

                                fillPathHelper.StartPoint.CompleteTransitions(
                                    nameof(fillPathHelper.StartPoint.Y), nameof(fillPathHelper.StartPoint.X));
                                fillPathHelper.StartSegment.CompleteTransitions(
                                    nameof(fillPathHelper.StartSegment.Y), nameof(fillPathHelper.StartSegment.X));
                            }

                            fillPathHelper.StartPoint.X = IsClosed ? (float)data.X0 : scaler.CenterX;
                            fillPathHelper.StartPoint.Y = IsClosed ? (float)data.Y0 : scaler.CenterY;
                            fillPathHelper.Path.AddLast(fillPathHelper.StartPoint);

                            fillPathHelper.StartSegment.X = (float)data.X0;
                            fillPathHelper.StartSegment.Y = (float)data.Y0;
                            if (!IsClosed) fillPathHelper.Path.AddLast(fillPathHelper.StartSegment);
                        }

                        fillPathHelper.Path.AddLast(visual.Bezier);

                        if (data.IsLast)
                        {
                            fillPathHelper.EndSegment.X = scaler.CenterX;
                            fillPathHelper.EndSegment.Y = scaler.CenterY;
                            if (!IsClosed) fillPathHelper.Path.AddLast(fillPathHelper.EndSegment);

                            if (wasFillInitialized)
                                fillPathHelper.EndSegment.CompleteTransitions(
                                    nameof(fillPathHelper.EndSegment.Y), nameof(fillPathHelper.EndSegment.X));
                        }
                    }
                    if (Stroke is not null)
                    {
                        if (data.IsFirst)
                        {
                            if (wasStrokeInitialized)
                            {
                                strokePathHelper.StartPoint.X = scaler.CenterX;
                                strokePathHelper.StartPoint.Y = scaler.CenterY;

                                strokePathHelper.StartPoint.CompleteTransitions(
                                   nameof(strokePathHelper.StartPoint.Y), nameof(strokePathHelper.StartPoint.X));
                            }

                            strokePathHelper.StartPoint.X = (float)data.X0;
                            strokePathHelper.StartPoint.Y = (float)data.Y0;

                            strokePathHelper.Path.AddLast(strokePathHelper.StartPoint);
                        }

                        strokePathHelper.Path.AddLast(visual.Bezier);
                    }

                    visual.Geometry.X = x - hgs;
                    visual.Geometry.Y = y - hgs;
                    visual.Geometry.Width = gs;
                    visual.Geometry.Height = gs;
                    visual.Geometry.RemoveOnCompleted = false;

                    visual.FillPath = fillPathHelper.Path;
                    visual.StrokePath = strokePathHelper.Path;

                    var hags = gs < 8 ? 8 : gs;

                    data.TargetPoint.Context.HoverArea = new RectangleHoverArea()
                        .SetDimensions(x - hgs, y - hgs + 2 * sw, hags, hags + 2 * sw);

                    _ = toDeletePoints.Remove(data.TargetPoint);

                    if (DataLabelsPaint is not null)
                    {
                        var label = (TLabel?)data.TargetPoint.Context.Label;

                        var actualRotation = r +
                            (isTangent ? scaler.GetAngle(data.TargetPoint.SecondaryValue) - 90 : 0) +
                            (isCotangent ? scaler.GetAngle(data.TargetPoint.SecondaryValue) : 0);

                        if ((isTangent || isCotangent) && ((actualRotation + 90) % 360) > 180)
                            actualRotation += 180;

                        if (label is null)
                        {
                            var l = new TLabel { X = x - hgs, Y = scaler.CenterY - hgs, RotateTransform = (float)actualRotation };

                            _ = l.TransitionateProperties(nameof(l.X), nameof(l.Y))
                                .WithAnimation(animation =>
                                    animation
                                        .WithDuration(AnimationsSpeed ?? polarChart.AnimationsSpeed)
                                        .WithEasingFunction(EasingFunction ?? polarChart.EasingFunction));

                            l.CompleteAllTransitions();
                            label = l;
                            data.TargetPoint.Context.Label = l;
                        }

                        DataLabelsPaint.AddGeometryToPaintTask(polarChart.Canvas, label);
                        label.Text = DataLabelsFormatter(
                            new TypedChartPoint<TModel, LineBezierVisualPoint<TDrawingContext, TVisual, TBezierSegment, TPathArgs>, TLabel, TDrawingContext>(data.TargetPoint));
                        label.TextSize = dls;
                        label.Padding = DataLabelsPadding;
                        label.RotateTransform = actualRotation;

                        var rad = Math.Sqrt(Math.Pow(cp.X - scaler.CenterX, 2) + Math.Pow(cp.Y - scaler.CenterY, 2));

                        var labelPosition = GetLabelPolarPosition(
                            scaler.CenterX, scaler.CenterY, (float)rad, scaler.GetAngle(data.TargetPoint.SecondaryValue),
                            label.Measure(DataLabelsPaint), (float)GeometrySize, DataLabelsPosition);

                        label.X = labelPosition.X;
                        label.Y = labelPosition.Y;
                    }

                    OnPointMeasured(data.TargetPoint);
                }

                if (GeometryFill is not null)
                {
                    polarChart.Canvas.AddDrawableTask(GeometryFill);
                    GeometryFill.SetClipRectangle(polarChart.Canvas, new LvcRectangle(drawLocation, drawMarginSize));
                    GeometryFill.ZIndex = actualZIndex + 0.3;
                }
                if (GeometryStroke is not null)
                {
                    polarChart.Canvas.AddDrawableTask(GeometryStroke);
                    GeometryStroke.SetClipRectangle(polarChart.Canvas, new LvcRectangle(drawLocation, drawMarginSize));
                    GeometryStroke.ZIndex = actualZIndex + 0.4;
                }
                segmentI++;
            }

            while (segmentI > fillPathHelperContainer.Count)
            {
                var iFill = fillPathHelperContainer.Count - 1;
                var fillHelper = fillPathHelperContainer[iFill];
                if (Fill is not null) Fill.RemoveGeometryFromPainTask(polarChart.Canvas, fillHelper.Path);
                fillPathHelperContainer.RemoveAt(iFill);

                var iStroke = strokePathHelperContainer.Count - 1;
                var strokeHelper = strokePathHelperContainer[iStroke];
                if (Stroke is not null) Stroke.RemoveGeometryFromPainTask(polarChart.Canvas, strokeHelper.Path);
                strokePathHelperContainer.RemoveAt(iStroke);
            }

            if (DataLabelsPaint is not null)
            {
                polarChart.Canvas.AddDrawableTask(DataLabelsPaint);
                //DataLabelsPaint.SetClipRectangle(polarChart.Canvas, new LvcRectangle(drawLocation, drawMarginSize));
                DataLabelsPaint.ZIndex = actualZIndex + 0.5;
            }

            foreach (var point in toDeletePoints)
            {
                if (point.Context.Chart != polarChart.View) continue;
                SoftDeleteOrDisposePoint(point, scaler);
                _ = everFetched.Remove(point);
            }
        }

        /// <inheritdoc cref="IPolarSeries{TDrawingContext}.GetBounds(PolarChart{TDrawingContext}, IPolarAxis, IPolarAxis)"/>
        public virtual SeriesBounds GetBounds(
            PolarChart<TDrawingContext> chart, IPolarAxis angleAxis, IPolarAxis radiusAxis)
        {
            var baseSeriesBounds = DataFactory is null
                ? throw new Exception("A data provider is required")
                : DataFactory.GetCartesianBounds(chart, this, angleAxis, radiusAxis);

            if (baseSeriesBounds.HasData) return baseSeriesBounds;
            var baseBounds = baseSeriesBounds.Bounds;

            var tickPrimary = radiusAxis.GetTick(chart, baseBounds.VisiblePrimaryBounds);
            var tickSecondary = angleAxis.GetTick(chart, baseBounds.VisibleSecondaryBounds);

            var ts = tickSecondary.Value * DataPadding.X;
            var tp = tickPrimary.Value * DataPadding.Y;

            if (baseBounds.VisibleSecondaryBounds.Delta == 0)
            {
                var ms = baseBounds.VisibleSecondaryBounds.Min == 0 ? 1 : baseBounds.VisibleSecondaryBounds.Min;
                ts = 0.1 * ms * DataPadding.X;
            }

            if (baseBounds.VisiblePrimaryBounds.Delta == 0)
            {
                var mp = baseBounds.VisiblePrimaryBounds.Min == 0 ? 1 : baseBounds.VisiblePrimaryBounds.Min;
                tp = 0.1 * mp * DataPadding.Y;
            }

            var rgs = GeometrySize * 0.5f + (Stroke?.StrokeThickness ?? 0);

            return
                new SeriesBounds(
                    new DimensionalBounds
                    {
                        SecondaryBounds = new Bounds
                        {
                            Max = baseBounds.SecondaryBounds.Max,
                            Min = baseBounds.SecondaryBounds.Min,
                            PaddingMax = ts,
                            PaddingMin = 0,
                            RequestedGeometrySize = rgs
                        },
                        PrimaryBounds = new Bounds
                        {
                            Max = baseBounds.PrimaryBounds.Max,
                            Min = baseBounds.PrimaryBounds.Min,
                            PaddingMax = tp,
                            PaddingMin = tp,
                            RequestedGeometrySize = rgs
                        },
                        VisibleSecondaryBounds = new Bounds
                        {
                            Max = baseBounds.VisibleSecondaryBounds.Max,
                            Min = baseBounds.VisibleSecondaryBounds.Min,
                        },
                        VisiblePrimaryBounds = new Bounds
                        {
                            Max = baseBounds.VisiblePrimaryBounds.Max,
                            Min = baseBounds.VisiblePrimaryBounds.Min
                        },
                        MinDeltaPrimary = baseBounds.MinDeltaPrimary,
                        MinDeltaSecondary = baseBounds.MinDeltaSecondary
                    },
                    false);
        }

        /// <inheritdoc cref="IChartSeries{TDrawingContext}.MiniatureEquals(IChartSeries{TDrawingContext})"/>
        public override bool MiniatureEquals(IChartSeries<TDrawingContext> series)
        {
            return series is StrokeAndFillCartesianSeries<TModel, TVisual, TLabel, TDrawingContext> sfSeries &&
                Name == series.Name && Fill == sfSeries.Fill && Stroke == sfSeries.Stroke;
        }

        /// <summary>
        /// Called when [paint changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns></returns>
        protected override void OnPaintChanged(string? propertyName)
        {
            OnSeriesMiniatureChanged();
            OnPropertyChanged();
        }

        /// <summary>
        /// Sets the default path transitions.
        /// </summary>
        /// <param name="areaHelper">The area helper.</param>
        /// <param name="defaultAnimation">The default animation.</param>
        /// <returns></returns>
        protected virtual void SetDefaultPathTransitions(
            AreaHelper<TDrawingContext, TPathGeometry, TLineSegment, TMoveToCommand, TPathArgs> areaHelper,
            Animation defaultAnimation)
        {
            _ = areaHelper.StartPoint
                .TransitionateProperties(nameof(areaHelper.StartPoint.X), nameof(areaHelper.StartPoint.Y))
                .WithAnimation(defaultAnimation)
                .CompleteCurrentTransitions();

            _ = areaHelper.StartSegment
                .TransitionateProperties(nameof(areaHelper.StartSegment.X), nameof(areaHelper.StartSegment.Y))
                .WithAnimation(defaultAnimation)
                .CompleteCurrentTransitions();

            _ = areaHelper.EndSegment
                .TransitionateProperties(nameof(areaHelper.EndSegment.X), nameof(areaHelper.EndSegment.Y))
                .WithAnimation(defaultAnimation)
                .CompleteCurrentTransitions();
        }

        /// <inheritdoc cref="ChartSeries{TModel, TVisual, TLabel, TDrawingContext}.OnSeriesMiniatureChanged"/>
        protected override void OnSeriesMiniatureChanged()
        {
            var context = new CanvasSchedule<TDrawingContext>();
            var lss = (float)LegendShapeSize;
            var w = LegendShapeSize;
            var sh = 0f;

            if (_geometryStroke is not null)
            {
                var strokeClone = _geometryStroke.CloneTask();
                var st = _geometryStroke.StrokeThickness;
                if (st > MaxSeriesStroke)
                {
                    st = MaxSeriesStroke;
                    strokeClone.StrokeThickness = MaxSeriesStroke;
                }

                var visual = new TVisual
                {
                    X = st + MaxSeriesStroke - st,
                    Y = st + MaxSeriesStroke - st,
                    Height = lss,
                    Width = lss
                };
                sh = st;
                strokeClone.ZIndex = 1;
                context.PaintSchedules.Add(new PaintSchedule<TDrawingContext>(strokeClone, visual));
            }
            else if (Stroke is not null)
            {
                var strokeClone = Stroke.CloneTask();
                var st = strokeClone.StrokeThickness;
                if (st > MaxSeriesStroke)
                {
                    st = MaxSeriesStroke;
                    strokeClone.StrokeThickness = MaxSeriesStroke;
                }

                var visual = new TVisual
                {
                    X = st + MaxSeriesStroke - st,
                    Y = st + MaxSeriesStroke - st,
                    Height = lss,
                    Width = lss
                };
                sh = st;
                strokeClone.ZIndex = 1;
                context.PaintSchedules.Add(new PaintSchedule<TDrawingContext>(strokeClone, visual));
            }

            if (_geometryFill is not null)
            {
                var fillClone = _geometryFill.CloneTask();
                var visual = new TVisual { X = sh + MaxSeriesStroke - sh, Y = sh + MaxSeriesStroke - sh, Height = lss, Width = lss };
                context.PaintSchedules.Add(new PaintSchedule<TDrawingContext>(fillClone, visual));
            }
            else if (Fill is not null)
            {
                var fillClone = Fill.CloneTask();
                var visual = new TVisual { X = sh + MaxSeriesStroke - sh, Y = sh + MaxSeriesStroke - sh, Height = lss, Width = lss };
                context.PaintSchedules.Add(new PaintSchedule<TDrawingContext>(fillClone, visual));
            }

            context.Width = w + MaxSeriesStroke * 2;
            context.Height = w + MaxSeriesStroke * 2;

            CanvasSchedule = context;
        }

        private IEnumerable<BezierData> GetSpline(
            ChartPoint[] points,
            PolarScaler scaler,
            StackPosition<TDrawingContext>? stacker)
        {
            if (points.Length == 0) yield break;

            ChartPoint previous, current, next, next2;

            for (var i = 0; i < points.Length; i++)
            {
                previous = points[i - 1 < 0 ? 0 : i - 1];
                current = points[i];
                next = points[i + 1 > points.Length - 1 ? points.Length - 1 : i + 1];
                next2 = points[i + 2 > points.Length - 1 ? points.Length - 1 : i + 2];

                var pys = 0d;
                var cys = 0d;
                var nys = 0d;
                var nnys = 0d;

                if (stacker is not null)
                {
                    pys = stacker.GetStack(previous).Start;
                    cys = stacker.GetStack(current).Start;
                    nys = stacker.GetStack(next).Start;
                    nnys = stacker.GetStack(next2).Start;
                }

                var xc1 = (previous.SecondaryValue + current.SecondaryValue) / 2.0f;
                var yc1 = (previous.PrimaryValue + pys + current.PrimaryValue + cys) / 2.0f;
                var xc2 = (current.SecondaryValue + next.SecondaryValue) / 2.0f;
                var yc2 = (current.PrimaryValue + cys + next.PrimaryValue + nys) / 2.0f;
                var xc3 = (next.SecondaryValue + next2.SecondaryValue) / 2.0f;
                var yc3 = (next.PrimaryValue + nys + next2.PrimaryValue + nnys) / 2.0f;

                var len1 = (float)Math.Sqrt(
                    (current.SecondaryValue - previous.SecondaryValue) *
                    (current.SecondaryValue - previous.SecondaryValue) +
                    (current.PrimaryValue + cys - previous.PrimaryValue + pys) * (current.PrimaryValue + cys - previous.PrimaryValue + pys));
                var len2 = (float)Math.Sqrt(
                    (next.SecondaryValue - current.SecondaryValue) *
                    (next.SecondaryValue - current.SecondaryValue) +
                    (next.PrimaryValue + nys - current.PrimaryValue + cys) * (next.PrimaryValue + nys - current.PrimaryValue + cys));
                var len3 = (float)Math.Sqrt(
                    (next2.SecondaryValue - next.SecondaryValue) *
                    (next2.SecondaryValue - next.SecondaryValue) +
                    (next2.PrimaryValue + nnys - next.PrimaryValue + nys) * (next2.PrimaryValue + nnys - next.PrimaryValue + nys));

                var k1 = len1 / (len1 + len2);
                var k2 = len2 / (len2 + len3);

                if (float.IsNaN(k1)) k1 = 0f;
                if (float.IsNaN(k2)) k2 = 0f;

                var xm1 = xc1 + (xc2 - xc1) * k1;
                var ym1 = yc1 + (yc2 - yc1) * k1;
                var xm2 = xc2 + (xc3 - xc2) * k2;
                var ym2 = yc2 + (yc3 - yc2) * k2;

                var c1X = xm1 + (xc2 - xm1) * _lineSmoothness + current.SecondaryValue - xm1;
                var c1Y = ym1 + (yc2 - ym1) * _lineSmoothness + current.PrimaryValue + cys - ym1;
                var c2X = xm2 + (xc2 - xm2) * _lineSmoothness + next.SecondaryValue - xm2;
                var c2Y = ym2 + (yc2 - ym2) * _lineSmoothness + next.PrimaryValue + nys - ym2;

                double x0, y0;

                if (i == 0)
                {
                    x0 = current.SecondaryValue;
                    y0 = current.PrimaryValue + cys;
                }
                else
                {
                    x0 = c1X;
                    y0 = c1Y;
                }

                var s0 = scaler.ToPixels(x0, y0);
                var s1 = scaler.ToPixels(c2X, c2Y);
                var s2 = scaler.ToPixels(next.SecondaryValue, next.PrimaryValue);

                yield return new BezierData(points[i])
                {
                    IsFirst = i == 0,
                    IsLast = i == points.Length - 1,
                    X0 = s0.X,
                    Y0 = s0.Y,
                    X1 = s1.X,
                    Y1 = s1.Y,
                    X2 = s2.X,
                    Y2 = s2.Y,
                    OriginalData = new BezierData(points[i])
                    {
                        X0 = x0,
                        Y0 = y0,
                        X1 = c2X,
                        Y1 = c2Y,
                        X2 = next.SecondaryValue,
                        Y2 = next.PrimaryValue + nys,
                    }
                };
            }
        }

        /// <inheritdoc cref="Series{TModel, TVisual, TLabel, TDrawingContext}.SetDefaultPointTransitions(ChartPoint)"/>
        protected override void SetDefaultPointTransitions(ChartPoint chartPoint)
        {
            var chart = chartPoint.Context.Chart;

            if (chartPoint.Context.Visual is not LineBezierVisualPoint<TDrawingContext, TVisual, TBezierSegment, TPathArgs> visual)
                throw new Exception("Unable to initialize the point instance.");

            _ = visual.Geometry
                .TransitionateProperties(
                    nameof(visual.Geometry.X),
                    nameof(visual.Geometry.Y),
                    nameof(visual.Geometry.Width),
                    nameof(visual.Geometry.Height))
                .WithAnimation(animation =>
                    animation
                        .WithDuration(AnimationsSpeed ?? chart.AnimationsSpeed)
                        .WithEasingFunction(EasingFunction ?? chart.EasingFunction))
                .CompleteCurrentTransitions();

            _ = visual.Bezier
                .TransitionateProperties(
                    nameof(visual.Bezier.X0),
                    nameof(visual.Bezier.Y0),
                    nameof(visual.Bezier.X1),
                    nameof(visual.Bezier.Y1),
                    nameof(visual.Bezier.X2),
                    nameof(visual.Bezier.Y2))
                .WithAnimation(animation =>
                    animation
                        .WithDuration(AnimationsSpeed ?? chart.AnimationsSpeed)
                        .WithEasingFunction(EasingFunction ?? chart.EasingFunction))
                .CompleteCurrentTransitions();
        }

        /// <summary>
        /// Softs the delete point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="scaler">The scaler.</param>
        protected virtual void SoftDeleteOrDisposePoint(ChartPoint point, PolarScaler scaler)
        {
            var visual = (LineBezierVisualPoint<TDrawingContext, TVisual, TBezierSegment, TPathArgs>?)point.Context.Visual;
            if (visual is null) return;
            if (DataFactory is null) throw new Exception("Data provider not found");

            var p = scaler.ToPixels(point);
            var x = p.X;
            var y = p.Y;

            visual.Geometry.X = x;
            visual.Geometry.Y = y;
            visual.Geometry.Height = 0;
            visual.Geometry.Width = 0;
            visual.Geometry.RemoveOnCompleted = true;

            DataFactory.DisposePoint(point);

            var label = (TLabel?)point.Context.Label;
            if (label is null) return;

            label.TextSize = 1;
            label.RemoveOnCompleted = true;
        }

        /// <inheritdoc cref="Series{TModel, TVisual, TLabel, TDrawingContext}.SoftDeleteOrDispose(IChartView)"/>
        public override void SoftDeleteOrDispose(IChartView chart)
        {
            var core = ((IPolarChartView<TDrawingContext>)chart).Core;

            var scale = new PolarScaler(
                core.DrawMarginLocation, core.DrawMarginSize, core.AngleAxes[ScalesAngleAt], core.RadiusAxes[ScalesRadiusAt],
                core.InnerRadius, core.InitialRotation, core.TotalAnge);

            var deleted = new List<ChartPoint>();
            foreach (var point in everFetched)
            {
                if (point.Context.Chart != chart) continue;

                SoftDeleteOrDisposePoint(point, scale);
                deleted.Add(point);
            }

            foreach (var pt in GetPaintTasks())
            {
                if (pt is not null) core.Canvas.RemovePaintTask(pt);
            }

            foreach (var item in deleted) _ = everFetched.Remove(item);

            var canvas = ((IPolarChartView<TDrawingContext>)chart).CoreCanvas;

            if (Fill is not null)
            {
                foreach (var activeChartContainer in _fillPathHelperDictionary.ToArray())
                    foreach (var pathHelper in activeChartContainer.Value.ToArray())
                        Fill.RemoveGeometryFromPainTask(canvas, pathHelper.Path);
            }

            if (Stroke is not null)
            {
                foreach (var activeChartContainer in _strokePathHelperDictionary.ToArray())
                    foreach (var pathHelper in activeChartContainer.Value.ToArray())
                        Stroke.RemoveGeometryFromPainTask(canvas, pathHelper.Path);
            }

            if (GeometryFill is not null) canvas.RemovePaintTask(GeometryFill);
            if (GeometryStroke is not null) canvas.RemovePaintTask(GeometryStroke);

            OnVisibilityChanged();
        }

        /// <summary>
        /// Gets the paint tasks.
        /// </summary>
        /// <returns></returns>
        protected override IPaint<TDrawingContext>?[] GetPaintTasks()
        {
            return new[] { Stroke, Fill, _geometryFill, _geometryStroke, DataLabelsPaint, hoverPaint };
        }

        /// <summary>
        /// Gets the label polar position.
        /// </summary>
        /// <param name="centerX">The center x.</param>
        /// <param name="centerY">The center y.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="angle">The start angle.</param>
        /// <param name="labelSize">Size of the label.</param>
        /// <param name="geometrySize">The geometry size.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        protected virtual LvcPoint GetLabelPolarPosition(
            float centerX,
            float centerY,
            float radius,
            float angle,
            LvcSize labelSize,
            float geometrySize,
            PolarLabelsPosition position)
        {
            const float toRadians = (float)(Math.PI / 180);
            float actualAngle = 0;

            switch (position)
            {
                case PolarLabelsPosition.End:
                    actualAngle = angle;
                    radius += (float)Math.Sqrt(
                        Math.Pow(labelSize.Width + geometrySize * 0.5f, 2) +
                        Math.Pow(labelSize.Height + geometrySize * 0.5f, 2)) * 0.5f;
                    break;
                case PolarLabelsPosition.Start:
                    actualAngle = angle;
                    radius -= (float)Math.Sqrt(
                        Math.Pow(labelSize.Width + geometrySize * 0.5f, 2) +
                        Math.Pow(labelSize.Height + geometrySize * 0.5f, 2)) * 0.5f;
                    break;
                case PolarLabelsPosition.Middle:
                    actualAngle = angle;
                    break;
                case PolarLabelsPosition.ChartCenter:
                    return new LvcPoint(centerX, centerY);
                default:
                    break;
            }

            actualAngle %= 360;
            if (actualAngle < 0) actualAngle += 360;
            actualAngle *= toRadians;

            return new LvcPoint(
                 (float)(centerX + Math.Cos(actualAngle) * radius),
                 (float)(centerY + Math.Sin(actualAngle) * radius));
        }

        private IEnumerable<ChartPoint[]> SplitEachNull(
            ChartPoint[] points,
            PolarScaler scaler)
        {
            var l = new List<ChartPoint>(points.Length);

            foreach (var point in points)
            {
                if (point.IsNull)
                {
                    if (point.Context.Visual is LineBezierVisualPoint<TDrawingContext, TVisual, TBezierSegment, TPathArgs> visual)
                    {
                        var s = scaler.ToPixels(point);
                        var x = s.X;
                        var y = s.Y;
                        var gs = _geometrySize;
                        var hgs = gs / 2f;
                        var sw = Stroke?.StrokeThickness ?? 0;
                        visual.Geometry.X = x - hgs;
                        visual.Geometry.Y = y - hgs;
                        visual.Geometry.Width = gs;
                        visual.Geometry.Height = gs;
                        visual.Geometry.RemoveOnCompleted = true;
                        point.Context.Visual = null;
                    }

                    if (l.Count > 0) yield return l.ToArray();
                    l = new List<ChartPoint>(points.Length);
                    continue;
                }

                l.Add(point);
            }

            if (l.Count > 0) yield return l.ToArray();
        }
    }
}
