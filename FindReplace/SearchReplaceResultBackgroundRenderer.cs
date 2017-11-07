using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FindReplace
{
    // original from: https://github.com/aelij/RoslynPad/blob/master/src/RoslynPad.Editor.Windows/SearchReplacePanel.cs
    class SearchReplaceResultBackgroundRenderer : IBackgroundRenderer
    {
        private Brush _markerBrush;
        private Pen _markerPen;

        public TextSegmentCollection<TextSegment> CurrentResults { get; } = new TextSegmentCollection<TextSegment>();

        public KnownLayer Layer
        {
            get
            {
                // draw behind selection
                return KnownLayer.Selection;
            }
        }

        public SearchReplaceResultBackgroundRenderer()
        {
            _markerBrush = Brushes.LightGreen;
            _markerPen = new Pen(_markerBrush, 1);
        }

        public Brush MarkerBrush
        {
            get { return _markerBrush; }
            set
            {
                _markerBrush = value;
                _markerPen = new Pen(_markerBrush, 1);
            }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            if (drawingContext == null)
                throw new ArgumentNullException("drawingContext");

            if (CurrentResults == null || !textView.VisualLinesValid)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            var viewStart = visualLines.First().FirstDocumentLine.Offset;
            var viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

            foreach (var result in CurrentResults.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                var geoBuilder = new BackgroundGeometryBuilder
                {
                    //BorderThickness = markerPen != null ? markerPen.Thickness : 0,
                    AlignToWholePixels = true,
                    CornerRadius = 3
                };
                geoBuilder.AddSegment(textView, result);
                var geometry = geoBuilder.CreateGeometry();
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(_markerBrush, _markerPen, geometry);
                }
            }
        }
    }
}
