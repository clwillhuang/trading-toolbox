#region Using declarations
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;
using System.Windows;
#endregion>

namespace NinjaTrader.NinjaScript.Indicators
{
    [Description("Draws three boxes per day, with each box determined by the high and low during its specified time range of the day.")]
    public class DailyHighLow : Indicator
    {
        #region Variables

        /// <summary>
		/// Brush used to draw outline of all high and low boxes
		/// </summary>
        private System.Windows.Media.Brush boxOutlineBrush;

        /// <summary>
		/// Brush used to fill in all high and low boxes
		/// </summary>
        private System.Windows.Media.Brush boxFillBrush;
		
		/// <summary>
		/// Opacity of the high and low boxes (100 = opaque, 0 = transparent)
		/// </summary>
		private int boxOpacity;
		
        /// <summary>
        /// Data for the three high and low boxes graphed
        /// </summary>
		private HighLowBox box1, box2, box3;
		private HighLowBox[] boxes;
		
        /// <summary>
        /// The horizontal x offset for the box boundaries, to prevent overlap with candlewicks
        /// </summary>
		private TimeSpan timeOffset;

        /// <summary>
        /// The font used to draw time labels
        /// </summary>
		private SimpleFont timeLabelFont = new SimpleFont("Arial", 11); 

        /// <summary>
        /// Label each box with the open and close times?
        /// </summary>
        private bool showTimeLabels;

        #endregion

        public class HighLowBox {
            public TimeSpan openTime;
            public TimeSpan closeTime;
            public double low, high; 
            public DateTime openDateTime;
            public DateTime closeDateTime;
			public DateTime resetTime;
            public string rectangleTag;
            public DateTime MakeDateTime(TimeSpan timeSpan, DateTime today)
            {
                return new DateTime(today.Year, today.Month, today.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds); 
            }
            public HighLowBox(TimeSpan openTime, TimeSpan closeTime)
            {
                this.openTime = openTime;
                this.closeTime = closeTime;
                this.openDateTime = new DateTime(2021, 1, 1, openTime.Hours, openTime.Minutes, openTime.Seconds); 
                this.closeDateTime = new DateTime(2021, 1, 1, closeTime.Hours, closeTime.Minutes, closeTime.Seconds);   
                this.low = 0;
                this.high = 0;
                this.rectangleTag = "NULL";
            }
            
            public void ResetBox(double newLow, double newHigh, DateTime newDateTime) 
            {
				this.resetTime = newDateTime;
                this.openDateTime = MakeDateTime(openTime, newDateTime); 
                this.closeDateTime = MakeDateTime(closeTime, newDateTime); 
                this.rectangleTag = this.openDateTime.ToString() + "to" + this.closeDateTime.ToString(); 
                this.low = newLow;
                this.high = newHigh;
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "DailyHighLow";
                Description = "Draws three boxes per day, with each box determined by the high and low during its specified time range of the day."; 

                box1 = new HighLowBox(new TimeSpan(3, 0, 0), new TimeSpan(8, 0, 0)); 
                box2 = new HighLowBox(new TimeSpan(9, 30, 0), new TimeSpan(15, 0, 0));
                box3 = new HighLowBox(new TimeSpan(17, 0, 0), new TimeSpan(23, 0, 0));
				boxes = new HighLowBox[] { box1, box2, box3 };

                this.boxFillBrush = Brushes.CornflowerBlue; 
                this.boxOutlineBrush = Brushes.CornflowerBlue;
				this.boxOpacity = 40; 
                this.showTimeLabels = false;

                IsOverlay                       = true; 
                IsChartOnly                     = true;
                IsAutoScale                     = false;
                IsSuspendedWhileInactive        = true; 
                Calculate                       = Calculate.OnEachTick; 
                DisplayInDataBox                = false;
            }
			if (State == State.Configure)
			{
				timeOffset = new TimeSpan(0, 0, 0); 
			}
        }

        protected override void OnBarUpdate()
        {
			if (CurrentBar < 2) return;
			if (timeOffset != null) 
				timeOffset = new TimeSpan(((Time[0] - Time[1]).Ticks) / 2);

            for (int i = 0; i < boxes.Length; i++)
            {
                HighLowBox box = boxes[i];
                if (this.IsFirstTickOfBar && Time[0].TimeOfDay.CompareTo(box.openTime) > 0 && box.resetTime.Day != Time[1].Day && Low[0] > 0) {
                    box.ResetBox(Low[0], High[0], Time[1]);
                } 
				else if (box.resetTime.Day != Time[1].Day && Time[0].TimeOfDay.CompareTo(box.openTime) > 0 && Low[0] > 0) 
				{
					box.ResetBox(Low[0], High[0], Time[1]);
				}
                if (Time[0].CompareTo(box.resetTime) >= 0 && Time[0].CompareTo(box.closeDateTime) < 0)
                {
					if (Low[0] > 0)
                    	box.low = Math.Min(box.low, Low[0]);
                    box.high = Math.Max(box.high, High[0]);
                    Draw.Rectangle(this, box.rectangleTag, false, box.resetTime.Add(this.timeOffset), box.low, box.closeDateTime.Add(timeOffset), box.high, this.boxOutlineBrush, this.boxFillBrush, this.boxOpacity); 
                    if (this.showTimeLabels)
                	    Draw.Text(this, box.rectangleTag + " text", false, box.openDateTime.ToString() + " to " + box.closeDateTime.ToString(), box.resetTime.Add(this.timeOffset), box.high + 2 * this.TickSize, 0, Brushes.White, this.timeLabelFont, TextAlignment.Left, Brushes.Transparent, Brushes.Transparent, 100); 
				}
            } 
        }

        [Display(Name="Box 1 Open Time", Description="The open time (HH:MM:SS) of the first box.", Order=1, GroupName="Indicator Parameters")]
        public string Box1OpenTime
        { 
            get { return box1.openTime.ToString(); } 
            set { box1.openTime = TimeSpan.Parse(value); } 
        }

        [Display(Name="Box 1 Close Time", Description="The closing time (HH:MM:SS) of the first box.", Order=2, GroupName="Indicator Parameters")]
        public string Box1CloseTime
        { 
            get { return box1.closeTime.ToString(); } 
            set { box1.closeTime = TimeSpan.Parse(value); } 
        }

        [Display(Name="Box 2 Open Time", Description="The open time (HH:MM:SS) of the second box.", Order=3, GroupName="Indicator Parameters")]
        public string Box2OpenTime
        { 
            get { return box2.openTime.ToString(); } 
            set { box2.openTime = TimeSpan.Parse(value); } 
        }

        [Display(Name="Box 2 Close Time", Description="The closing time (HH:MM:SS) of the second box.", Order=4, GroupName="Indicator Parameters")]
        public string Box2CloseTime
        { 
            get { return box2.closeTime.ToString(); } 
            set { box2.closeTime = TimeSpan.Parse(value); } 
        }

        [Display(Name="Box 3 Open Time", Description="The open time (HH:MM:SS) of the third box.", Order=5, GroupName="Indicator Parameters")]
        public string Box3OpenTime
        { 
            get { return box3.openTime.ToString(); } 
            set { box3.openTime = TimeSpan.Parse(value); } 
        }

        [Display(Name="Box 3 Close Time", Description="The closing time (HH:MM:SS) of the third box.", Order=6, GroupName="Indicator Parameters")]
        public string Box3CloseTime
        { 
            get { return box3.closeTime.ToString(); } 
            set { box3.closeTime = TimeSpan.Parse(value); } 
        }

        [Display(ResourceType = typeof(Custom.Resource), Name = "Box Outline Color", GroupName = "Color/Text Parameters", Order = 1, Description = "Brush color used to draw outlines of high and low boxes.")]
        public System.Windows.Media.Brush BoxOutlineBrush
        {
            get { return this.boxOutlineBrush; }
            set { this.boxOutlineBrush = value; }
        } 

        [Display(ResourceType = typeof(Custom.Resource), Name = "Box Fill Color", GroupName = "Color/Text Parameters", Order = 2, Description = "Brush color used to fill in the high and low boxes.")]
        public System.Windows.Media.Brush BoxFillBrush
        {
            get { return this.boxFillBrush; }
            set { this.boxFillBrush = value; }
        } 
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Box Opacity", GroupName = "Color/Text Parameters", Order = 3, Description = "Opacity of the fill area for all high and low boxes. It is a value between 0-100 where 100 = opaque and 0 = transparent")]
        public int BoxOpacity
        {
            get { return this.boxOpacity; }
            set { this.boxOpacity = value; }
        } 

        [Display(ResourceType = typeof(Custom.Resource), Name = "Show Time Labels?", GroupName = "Color/Text Parameters", Order = 4, Description = "Select whether to add text labels above the high and low boxes on the chart.")]
        public bool ShowTimeLabels
        {
            get { return this.showTimeLabels; }
            set { this.showTimeLabels = value; }
        } 
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private DailyHighLow[] cacheDailyHighLow;
		public DailyHighLow DailyHighLow()
		{
			return DailyHighLow(Input);
		}

		public DailyHighLow DailyHighLow(ISeries<double> input)
		{
			if (cacheDailyHighLow != null)
				for (int idx = 0; idx < cacheDailyHighLow.Length; idx++)
					if (cacheDailyHighLow[idx] != null &&  cacheDailyHighLow[idx].EqualsInput(input))
						return cacheDailyHighLow[idx];
			return CacheIndicator<DailyHighLow>(new DailyHighLow(), input, ref cacheDailyHighLow);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.DailyHighLow DailyHighLow()
		{
			return indicator.DailyHighLow(Input);
		}

		public Indicators.DailyHighLow DailyHighLow(ISeries<double> input )
		{
			return indicator.DailyHighLow(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.DailyHighLow DailyHighLow()
		{
			return indicator.DailyHighLow(Input);
		}

		public Indicators.DailyHighLow DailyHighLow(ISeries<double> input )
		{
			return indicator.DailyHighLow(input);
		}
	}
}

#endregion
