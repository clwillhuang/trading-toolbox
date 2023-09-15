#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// A helper indicator to chart lows based on Tokyo trading times.
    /// </summary>
    [Description("A helper indicator to chart lows based on Tokyo trading times. Blue line represents the low identified during the Tokyo time. Light blue lines define the permitted deviation above and below the value of the Tokyo Low.")]
    public class TwoMarkets : Indicator
    {
        #region Variables
       
        /// <summary>
        /// The first and last bars to find the Tokyo Low between
        /// </summary>
        protected TimeSpan startLowTime, endLowTime; 

        /// <summary>
        /// the deviation in ticks that is permitted between the torontoOpenTime and the Tokyo Low
        /// </summary>
        protected int allowedDeviation; 

        /// <summary>
        /// The bar that we will compare to the Tokyo Low to see if we can trade on this day
        /// </summary>
        protected TimeSpan torontoOpenTime; 
        /// <summary>
        /// Deprecated; was intended to exit trades if they are active past this time
        /// </summary>
        protected TimeSpan torontoEndOfDay;

        private double lastTokyoLow;
        private bool showIndicatorsOnChart;

        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
            Add(new Plot(new Pen(Color.Blue, 3), PlotStyle.Line, "TokyoLow"));
			Add(new Plot(new Pen(Color.LightBlue, 3), PlotStyle.Line, "DeviationAbove"));
			Add(new Plot(new Pen(Color.LightBlue, 3), PlotStyle.Line, "DeviationBelow"));
            Overlay				= true;
            CalculateOnBarClose = false;
            startLowTime = new TimeSpan(21, 0, 0);
            endLowTime = new TimeSpan(3, 0, 0); 
            torontoOpenTime = new TimeSpan(10, 0, 0);
            torontoEndOfDay = new TimeSpan(16, 0, 0); 
            allowedDeviation = 16;
        }
		
		private void DrawErrorText(string errorMessage)
		{
			this.DrawText("Error"+Time[0], errorMessage, 0, this.High[-1] + 1f, Color.Red); 
		}

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if (this.CurrentBar < this.BarsRequired) return; 
            #region Null Checks
            if (startLowTime == null)
			{
				this.DrawErrorText("BeginningTimeToLookforLow was not properly imported; check format. Calculations will not be made"); 
				return;
			}
			else if (endLowTime == null) 
			{
				this.DrawErrorText("FinishingTimeToLookforLow was not properly imported; check format. Calculations will not be made"); 
				return; 
			}
			else if (torontoOpenTime == null)
			{
				this.DrawErrorText("TorontoOpenTime was not properly imported; check format. Calculations will not be made"); 
				return; 	
			}
            else if (torontoEndOfDay == null)
            {
                this.DrawErrorText("TorontoClosingTime was not properly imported; check format. Calculations will not be made"); 
				return; 	
            }
            #endregion 
            TimeSpan t = new TimeSpan(Time[0].Hour, Time[0].Minute, 0); 

            if (t == startLowTime) 
                lastTokyoLow = this.Low[0];
            if (t > startLowTime || t <= endLowTime) {
                if (this.Low[0] < lastTokyoLow || lastTokyoLow == -1)
                {
                    lastTokyoLow = this.Low[0];
                    for (int i = 0;; i++)
                    {
                        if (i >= CurrentBar) break; 
                        TimeSpan backTime = new TimeSpan(Time[i].Hour, Time[i].Minute, 0); 
                        if (backTime < startLowTime && backTime > endLowTime) break;  
                        Values[0][i] = lastTokyoLow;
                    }
                }
				Values[0][0] = lastTokyoLow;
            }

            if (t > torontoOpenTime && t < torontoEndOfDay)
            {
                Values[1][0] = lastTokyoLow + (double)allowedDeviation * TickSize; 
                Values[2][0] = lastTokyoLow - (double)allowedDeviation * TickSize;
            }

            if (this.FirstTickOfBar) { 
				TimeSpan actualOpen = new TimeSpan(Time[1].Hour, Time[1].Minute, 0); 
                if (actualOpen == torontoOpenTime)
                {
                    if (Math.Abs(this.Open[0] - this.lastTokyoLow) <= allowedDeviation * TickSize)
                        DrawArrowUp("Valid Day " + this.Time[0], 0, this.Open[0], Color.Green); 
                    else
                        DrawArrowUp("Invalid Day " +  this.Time[0], 0, this.Open[0], Color.Red); 
                }
            }
            
        }

        #region Properties
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries TokyoLow
        {
            get { return Values[0]; }
        }

        [Browsable(false)]	
        [XmlIgnore()]		
        public DataSeries DeviationAbove
        {
            get { return Values[1]; }
        } 

        [Browsable(false)]	
        [XmlIgnore()]		
        public DataSeries DeviationBelow
        {
            get { return Values[2]; }
        } 

        [Description("The earliest time to look for a low, written in 24hr format. Default: \"21:00:00\" (9PM Toronto Time).")]
        [GridCategory("Parameters")]
		public string BeginningTimeToLookforLow {
			get { return startLowTime.ToString(); }
			set { startLowTime = TimeSpan.Parse(value); }
		} 
		
		[Description("The last time to look for a low, written in 24hr format. Default: \"03:00:00\" (3AM Toronto Time).")]
        [GridCategory("Parameters")]
		public string FinishingTimeToLookforLow {
			get { return endLowTime.ToString(); }
			set { endLowTime = TimeSpan.Parse(value); }
		}

        [Description("The time of the bar that will be compared against the Tokyo Low. Default: \"10:00:00\" (10AM Toronto Time)")]
        [GridCategory("Parameters")]
		public string TorontoOpeningTime {
			get { return this.torontoOpenTime.ToString(); }
			set { this.torontoOpenTime = TimeSpan.Parse(value); }
		}

        [Description("The time of the last bar in the Toronto trading day, after which no trades will be considered. Default: \"16:00:00\" (4PM Toronto Time)")]
        [GridCategory("Parameters")]
		public string TorontoClosingTime {
			get { return this.torontoEndOfDay.ToString(); }
			set { this.torontoEndOfDay = TimeSpan.Parse(value); }
		}

        [Description("Allowed deviation between Toronto open and Tokyo Low")]
        [GridCategory("Parameters")]
        public int AllowedDeviationBetweenOpenAndTokyoLow { 
            get { return allowedDeviation; }
            set { allowedDeviation = value; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private TwoMarkets[] cacheTwoMarkets = null;

        private static TwoMarkets checkTwoMarkets = new TwoMarkets();

        /// <summary>
        /// A helper indicator to chart the Tokyo Lows, which are used for the 10AMTokyoTime Strategy. Blue line represents the low identified during the Tokyo time. Light blue lines define the permitted deviation above and below the value of the Tokyo Low.
        /// </summary>
        /// <returns></returns>
        public TwoMarkets TwoMarkets(int allowedDeviationBetweenOpenAndTokyoLow, string beginningTimeToLookforLow, string finishingTimeToLookforLow, string torontoClosingTime, string torontoOpeningTime)
        {
            return TwoMarkets(Input, allowedDeviationBetweenOpenAndTokyoLow, beginningTimeToLookforLow, finishingTimeToLookforLow, torontoClosingTime, torontoOpeningTime);
        }

        /// <summary>
        /// A helper indicator to chart the Tokyo Lows, which are used for the 10AMTokyoTime Strategy. Blue line represents the low identified during the Tokyo time. Light blue lines define the permitted deviation above and below the value of the Tokyo Low.
        /// </summary>
        /// <returns></returns>
        public TwoMarkets TwoMarkets(Data.IDataSeries input, int allowedDeviationBetweenOpenAndTokyoLow, string beginningTimeToLookforLow, string finishingTimeToLookforLow, string torontoClosingTime, string torontoOpeningTime)
        {
            if (cacheTwoMarkets != null)
                for (int idx = 0; idx < cacheTwoMarkets.Length; idx++)
                    if (cacheTwoMarkets[idx].AllowedDeviationBetweenOpenAndTokyoLow == allowedDeviationBetweenOpenAndTokyoLow && cacheTwoMarkets[idx].BeginningTimeToLookforLow == beginningTimeToLookforLow && cacheTwoMarkets[idx].FinishingTimeToLookforLow == finishingTimeToLookforLow && cacheTwoMarkets[idx].TorontoClosingTime == torontoClosingTime && cacheTwoMarkets[idx].TorontoOpeningTime == torontoOpeningTime && cacheTwoMarkets[idx].EqualsInput(input))
                        return cacheTwoMarkets[idx];

            lock (checkTwoMarkets)
            {
                checkTwoMarkets.AllowedDeviationBetweenOpenAndTokyoLow = allowedDeviationBetweenOpenAndTokyoLow;
                allowedDeviationBetweenOpenAndTokyoLow = checkTwoMarkets.AllowedDeviationBetweenOpenAndTokyoLow;
                checkTwoMarkets.BeginningTimeToLookforLow = beginningTimeToLookforLow;
                beginningTimeToLookforLow = checkTwoMarkets.BeginningTimeToLookforLow;
                checkTwoMarkets.FinishingTimeToLookforLow = finishingTimeToLookforLow;
                finishingTimeToLookforLow = checkTwoMarkets.FinishingTimeToLookforLow;
                checkTwoMarkets.TorontoClosingTime = torontoClosingTime;
                torontoClosingTime = checkTwoMarkets.TorontoClosingTime;
                checkTwoMarkets.TorontoOpeningTime = torontoOpeningTime;
                torontoOpeningTime = checkTwoMarkets.TorontoOpeningTime;

                if (cacheTwoMarkets != null)
                    for (int idx = 0; idx < cacheTwoMarkets.Length; idx++)
                        if (cacheTwoMarkets[idx].AllowedDeviationBetweenOpenAndTokyoLow == allowedDeviationBetweenOpenAndTokyoLow && cacheTwoMarkets[idx].BeginningTimeToLookforLow == beginningTimeToLookforLow && cacheTwoMarkets[idx].FinishingTimeToLookforLow == finishingTimeToLookforLow && cacheTwoMarkets[idx].TorontoClosingTime == torontoClosingTime && cacheTwoMarkets[idx].TorontoOpeningTime == torontoOpeningTime && cacheTwoMarkets[idx].EqualsInput(input))
                            return cacheTwoMarkets[idx];

                TwoMarkets indicator = new TwoMarkets();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.AllowedDeviationBetweenOpenAndTokyoLow = allowedDeviationBetweenOpenAndTokyoLow;
                indicator.BeginningTimeToLookforLow = beginningTimeToLookforLow;
                indicator.FinishingTimeToLookforLow = finishingTimeToLookforLow;
                indicator.TorontoClosingTime = torontoClosingTime;
                indicator.TorontoOpeningTime = torontoOpeningTime;
                Indicators.Add(indicator);
                indicator.SetUp();

                TwoMarkets[] tmp = new TwoMarkets[cacheTwoMarkets == null ? 1 : cacheTwoMarkets.Length + 1];
                if (cacheTwoMarkets != null)
                    cacheTwoMarkets.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheTwoMarkets = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// A helper indicator to chart the Tokyo Lows, which are used for the 10AMTokyoTime Strategy. Blue line represents the low identified during the Tokyo time. Light blue lines define the permitted deviation above and below the value of the Tokyo Low.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.TwoMarkets TwoMarkets(int allowedDeviationBetweenOpenAndTokyoLow, string beginningTimeToLookforLow, string finishingTimeToLookforLow, string torontoClosingTime, string torontoOpeningTime)
        {
            return _indicator.TwoMarkets(Input, allowedDeviationBetweenOpenAndTokyoLow, beginningTimeToLookforLow, finishingTimeToLookforLow, torontoClosingTime, torontoOpeningTime);
        }

        /// <summary>
        /// A helper indicator to chart the Tokyo Lows, which are used for the 10AMTokyoTime Strategy. Blue line represents the low identified during the Tokyo time. Light blue lines define the permitted deviation above and below the value of the Tokyo Low.
        /// </summary>
        /// <returns></returns>
        public Indicator.TwoMarkets TwoMarkets(Data.IDataSeries input, int allowedDeviationBetweenOpenAndTokyoLow, string beginningTimeToLookforLow, string finishingTimeToLookforLow, string torontoClosingTime, string torontoOpeningTime)
        {
            return _indicator.TwoMarkets(input, allowedDeviationBetweenOpenAndTokyoLow, beginningTimeToLookforLow, finishingTimeToLookforLow, torontoClosingTime, torontoOpeningTime);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// A helper indicator to chart the Tokyo Lows, which are used for the 10AMTokyoTime Strategy. Blue line represents the low identified during the Tokyo time. Light blue lines define the permitted deviation above and below the value of the Tokyo Low.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.TwoMarkets TwoMarkets(int allowedDeviationBetweenOpenAndTokyoLow, string beginningTimeToLookforLow, string finishingTimeToLookforLow, string torontoClosingTime, string torontoOpeningTime)
        {
            return _indicator.TwoMarkets(Input, allowedDeviationBetweenOpenAndTokyoLow, beginningTimeToLookforLow, finishingTimeToLookforLow, torontoClosingTime, torontoOpeningTime);
        }

        /// <summary>
        /// A helper indicator to chart the Tokyo Lows, which are used for the 10AMTokyoTime Strategy. Blue line represents the low identified during the Tokyo time. Light blue lines define the permitted deviation above and below the value of the Tokyo Low.
        /// </summary>
        /// <returns></returns>
        public Indicator.TwoMarkets TwoMarkets(Data.IDataSeries input, int allowedDeviationBetweenOpenAndTokyoLow, string beginningTimeToLookforLow, string finishingTimeToLookforLow, string torontoClosingTime, string torontoOpeningTime)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.TwoMarkets(input, allowedDeviationBetweenOpenAndTokyoLow, beginningTimeToLookforLow, finishingTimeToLookforLow, torontoClosingTime, torontoOpeningTime);
        }
    }
}
#endregion
