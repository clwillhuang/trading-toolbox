#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.DrawingTools;
using System.ComponentModel.DataAnnotations;
#endregion>

namespace NinjaTrader.NinjaScript.Indicators
{
    [Description("Shows a databox generated from the low and high of the trading day (day is determined by last visible bar).")]
    public class PercentageDifference : Indicator
    {
        #region Variables
		/// <summary>
		/// The percentage of the difference between the high and low to subtract or add from the low or the high
		/// </summary>
        private double ratio = 0.50;
		/// <summary>
		/// A cache of calculated values, stored by datetime (always midnight)
		/// </summary>
        private Dictionary<DateTime, string> cache;
		/// <summary>
		/// The location of the databox within the chart window
		/// </summary>
        private TextPosition displayPosition;
		/// <summary>
		/// The brushes used to colour data box text, outline, and fill respectively
		/// </summary>
        private Brush textBrush, outlineBrush, fillBrush; 
        private int fontSize;
		/// <summary>
		/// The font size of the data box
		/// </summary>
        private SimpleFont boxFont;
		/// <summary>
		/// Opacity of the data box (0 = transparent, 100 = opaque)
		/// </summary>
        private int boxOpacity;

        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "PercentageDifference";
                Description = "Shows a databox generated from the low and high of the trading day (day is determined by last visible bar)."; 

                this.displayPosition = TextPosition.TopLeft;
                this.textBrush = Brushes.Black; 
                this.outlineBrush = Brushes.DarkBlue;
                this.fillBrush = Brushes.White; 
                this.fontSize = 14;
                this.boxOpacity = 80;

                IsOverlay                       = true; 
                IsChartOnly                     = true;
                IsAutoScale                     = false;
                IsSuspendedWhileInactive        = true; 
                Calculate                       = Calculate.OnBarClose; 
                DisplayInDataBox                = false;
            }
            else if (State == State.Configure)
            {
                this.cache = new Dictionary<DateTime, string>(); 
                this.boxFont = new SimpleFont("Arial", this.fontSize);
            }
        }

        protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            int lastBarIdx = Math.Min(ChartBars.ToIndex, CurrentBar-1); 
            int firstBarIdx = ChartBars.FromIndex; 

            if (lastBarIdx < 0 || firstBarIdx < 0) return; 
            string toPrint; 

            DateTime lastDayPainted = Bars.GetTime(lastBarIdx).Date; 
            try {
                if (cache.ContainsKey(lastDayPainted))
                {
                    toPrint = cache[lastDayPainted]; 
                }
                else 
                {
                    double keyValue = Math.Round(GetKeyValue(lastDayPainted), 2); 
                    double close = Bars.GetClose(Bars.GetBar(lastDayPainted.AddHours(16))); 
                    double difference = Math.Abs(keyValue - close); 
                    toPrint = String.Format("{0}\nPerc%: {1}\nClose: {2}\nDifference: {3}", lastDayPainted.ToShortDateString(),keyValue, close, Math.Round(difference, 2)); 
                    cache.Add(lastDayPainted, toPrint); 
                }  
            }
            catch (InvalidOperationException)
            {
                toPrint = "Unable to display values for day: " + lastDayPainted.ToString();
            }
            Draw.TextFixed(this, "Percentage Diff", toPrint, displayPosition, this.textBrush, this.boxFont, this.outlineBrush, this.fillBrush, this.boxOpacity); 
        }

        private double GetKeyValue(DateTime lastTimePainted)
        {
            double high = 0;
            double low = double.PositiveInfinity;
            bool? isRising = null;
            DateTime startTime = new DateTime(lastTimePainted.Year, lastTimePainted.Month, lastTimePainted.Day, 9, 0, 0); 
            DateTime endTime = new DateTime(lastTimePainted.Year, lastTimePainted.Month, lastTimePainted.Day, 16, 0, 0); 
            int startIdx = Bars.GetBar(startTime);
            int endIdx = Bars.GetBar(endTime); 
            for (int i = startIdx; i <= Math.Min(endIdx, CurrentBar - 1); i++)
            {
                if (Bars.GetHigh(i) > high)
                {
                    high = Bars.GetHigh(i);
                    isRising = false;
                }
                if (Bars.GetLow(i) < low)
                {
                    low = Bars.GetLow(i);
                    isRising = true;
                }
            }
            if (high == 0 || low == double.PositiveInfinity || isRising == null)
            {
                Print(this.Name + ": No values for the date: " + lastTimePainted.Date); 
                throw new InvalidOperationException(this.Name + ": No values for date: " + lastTimePainted.Date.ToString()); 
            }
            double result = 0;
            if (isRising.Value) result = low + ratio * (high - low); 
            else result = high - ratio * (high - low); 
            return result;
        }
    
        #region Parameters

        [Display(Name = "Font Size", GroupName = "Color/Text Parameters", Order = 1, Description = "Font size of the data box, in pts.")]
        [Range(1, 100)]
        public int FontSize
        {
            get { return this.fontSize; }
            set { this.fontSize = value; }
        } 

        [Display(ResourceType = typeof(Custom.Resource), Name = "Font Color", GroupName = "Color/Text Parameters", Order = 2, Description = "Color used to write text in the data box.")]
        public System.Windows.Media.Brush FontColor
        {
            get { return this.textBrush; }
            set { this.textBrush = value; }
        } 

        [Display(Name = "Databox Location", GroupName = "Color/Text Parameters", Order = 3, Description = "The location of the databox within the chart window.")]
        public TextPosition DisplayPosition
        {
            get { return this.displayPosition; }
            set { this.displayPosition = value; }
        } 

        [Display(ResourceType = typeof(Custom.Resource), Name = "Box Outline Color", GroupName = "Color/Text Parameters", Order = 4, Description = "Brush color used to outline the data box.")]
        public System.Windows.Media.Brush BoxOutlineBrush
        {
            get { return this.outlineBrush; }
            set { this.outlineBrush = value; }
        }  

        [Display(ResourceType = typeof(Custom.Resource), Name = "Box Fill Color", GroupName = "Color/Text Parameters", Order = 5, Description = "Brush color used to fill in the data box.")]
        public System.Windows.Media.Brush BoxFillBrush
        {
            get { return this.fillBrush; }
            set { this.fillBrush = value; }
        } 

        [Display(Name = "Box Opacity", GroupName = "Color/Text Parameters", Order = 6, Description = "The opacity of the data box. 0 = transparent, 100 = opaque.")]
        [Range(0, 100)]
        public int BoxOpacity
        {
            get { return this.boxOpacity; }
            set { this.boxOpacity = value; }
        } 

        [Display(Name = "Ratio/Percentage", GroupName = "Indicator Parameters", Order = 0, Description = "The percentage of the difference between the high and low that will be either added to the sessional low or subtracted from the sessional high.")]
        [Range(0, 100)]
        public double Ratio
        {
            get { return this.ratio; }
            set { this.ratio = value; }
        } 
            
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PercentageDifference[] cachePercentageDifference;
		public PercentageDifference PercentageDifference()
		{
			return PercentageDifference(Input);
		}

		public PercentageDifference PercentageDifference(ISeries<double> input)
		{
			if (cachePercentageDifference != null)
				for (int idx = 0; idx < cachePercentageDifference.Length; idx++)
					if (cachePercentageDifference[idx] != null &&  cachePercentageDifference[idx].EqualsInput(input))
						return cachePercentageDifference[idx];
			return CacheIndicator<PercentageDifference>(new PercentageDifference(), input, ref cachePercentageDifference);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PercentageDifference PercentageDifference()
		{
			return indicator.PercentageDifference(Input);
		}

		public Indicators.PercentageDifference PercentageDifference(ISeries<double> input )
		{
			return indicator.PercentageDifference(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PercentageDifference PercentageDifference()
		{
			return indicator.PercentageDifference(Input);
		}

		public Indicators.PercentageDifference PercentageDifference(ISeries<double> input )
		{
			return indicator.PercentageDifference(input);
		}
	}
}

#endregion
