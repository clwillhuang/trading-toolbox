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
    /// Generate a long or short signal and alert whenever three consecutive candles are either all green or red.
    /// </summary>
    [Description("Generate a long or short signal and alert whenever three consecutive candles are either all green or red.")]
    public class ThreeCandlesInRow : Indicator
    {
        #region Variables
        // Wizard generated variables
        private int requiredConsecutiveBars = 3; 
		private string destinationEmailAddress = "youremail@domain.com"; 
		private bool turnOnEmailAlerts = true; 
        // User defined variables (add any user defined variables below)
        #endregion

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
            Overlay				= true;
			this.CalculateOnBarClose = true;
			ChartOnly			= true;
            AutoScale			= false;
			DisplayInDataBox	= false;
			this.RequiredConsecutiveBars = 3; 
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if (this.CurrentBar < this.RequiredConsecutiveBars + 1)
				return; 
		
			
			bool ConsecutiveUp = true; 
            for (int i = 0; i < this.requiredConsecutiveBars && ConsecutiveUp; i++)
			{
				if (Close[i] <= Close[i+1])
					ConsecutiveUp = false; 
			}
			if (ConsecutiveUp) { 
				for (int i = 0; i < this.requiredConsecutiveBars; i++)
					this.DrawArrowUp(Time[i].ToString()+"up", i, Low[this.requiredConsecutiveBars-1] - 4 * this.TickSize, Color.Green);
				SendEmailAlert("Consec Green bars", this.requiredConsecutiveBars.ToString() + " consecutive green bars have closed above the previous");
			}
			
			bool ConsecutiveDown = true; 
			for (int i = 0; i < this.requiredConsecutiveBars && ConsecutiveDown; i++)
			{
				if (Close[i] >= Close[i+1])
					ConsecutiveDown = false; 
			}
			if (ConsecutiveDown) { 
				for (int i = 0; i < this.requiredConsecutiveBars; i++)
					this.DrawArrowDown(Time[i].ToString()+"down", i, High[this.requiredConsecutiveBars-1] + 4 * this.TickSize, Color.Red);
				SendEmailAlert("Consec Red bars", this.requiredConsecutiveBars.ToString() + " consecutive red bars have closed below the previous");
			}
        }
		
		private void SendEmailAlert(string title, string message) {
			if (this.turnOnEmailAlerts)
				SendMail(destinationEmailAddress, destinationEmailAddress, "ThreeCandlesInRow Alert: " + title, 
						"Alert from indicator ThreeCandlesInRow.\n" + 
						message + 
						"Instrument: " + Instrument.FullName + "\n" + 
						"Chart: " + Bars.Period.Value + " " + Bars.BarsType);
		}

        #region Properties

        [Description("The number of consecutive green/red bars to trigger an arrow print and email alert.")]
        [GridCategory("Alert Parameters")]
        public int RequiredConsecutiveBars
        {
            get { return requiredConsecutiveBars; }
            set { requiredConsecutiveBars = Math.Max(1, value); }
        }
		
		[Description("Enter the email to which you want indicator alerts to be sent to.")]
        [GridCategory("Alert Parameters")]
        public string DestinationEmailAddress
        {
            get { return destinationEmailAddress; }
            set { destinationEmailAddress = value; }
        }
		
		[Description("Turn on whether or not email alerts will be sent to the supplied email address.")]
        [GridCategory("Alert Parameters")]
        public bool TurnOnEmailAlerts
        {
            get { return turnOnEmailAlerts; }
            set { turnOnEmailAlerts = value; }
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
        private ThreeCandlesInRow[] cacheThreeCandlesInRow = null;

        private static ThreeCandlesInRow checkThreeCandlesInRow = new ThreeCandlesInRow();

        /// <summary>
        /// Generate a long or short signal and alert whenever three consecutive candles are either all green or red.
        /// </summary>
        /// <returns></returns>
        public ThreeCandlesInRow ThreeCandlesInRow(string destinationEmailAddress, int requiredConsecutiveBars, bool turnOnEmailAlerts)
        {
            return ThreeCandlesInRow(Input, destinationEmailAddress, requiredConsecutiveBars, turnOnEmailAlerts);
        }

        /// <summary>
        /// Generate a long or short signal and alert whenever three consecutive candles are either all green or red.
        /// </summary>
        /// <returns></returns>
        public ThreeCandlesInRow ThreeCandlesInRow(Data.IDataSeries input, string destinationEmailAddress, int requiredConsecutiveBars, bool turnOnEmailAlerts)
        {
            if (cacheThreeCandlesInRow != null)
                for (int idx = 0; idx < cacheThreeCandlesInRow.Length; idx++)
                    if (cacheThreeCandlesInRow[idx].DestinationEmailAddress == destinationEmailAddress && cacheThreeCandlesInRow[idx].RequiredConsecutiveBars == requiredConsecutiveBars && cacheThreeCandlesInRow[idx].TurnOnEmailAlerts == turnOnEmailAlerts && cacheThreeCandlesInRow[idx].EqualsInput(input))
                        return cacheThreeCandlesInRow[idx];

            lock (checkThreeCandlesInRow)
            {
                checkThreeCandlesInRow.DestinationEmailAddress = destinationEmailAddress;
                destinationEmailAddress = checkThreeCandlesInRow.DestinationEmailAddress;
                checkThreeCandlesInRow.RequiredConsecutiveBars = requiredConsecutiveBars;
                requiredConsecutiveBars = checkThreeCandlesInRow.RequiredConsecutiveBars;
                checkThreeCandlesInRow.TurnOnEmailAlerts = turnOnEmailAlerts;
                turnOnEmailAlerts = checkThreeCandlesInRow.TurnOnEmailAlerts;

                if (cacheThreeCandlesInRow != null)
                    for (int idx = 0; idx < cacheThreeCandlesInRow.Length; idx++)
                        if (cacheThreeCandlesInRow[idx].DestinationEmailAddress == destinationEmailAddress && cacheThreeCandlesInRow[idx].RequiredConsecutiveBars == requiredConsecutiveBars && cacheThreeCandlesInRow[idx].TurnOnEmailAlerts == turnOnEmailAlerts && cacheThreeCandlesInRow[idx].EqualsInput(input))
                            return cacheThreeCandlesInRow[idx];

                ThreeCandlesInRow indicator = new ThreeCandlesInRow();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.DestinationEmailAddress = destinationEmailAddress;
                indicator.RequiredConsecutiveBars = requiredConsecutiveBars;
                indicator.TurnOnEmailAlerts = turnOnEmailAlerts;
                Indicators.Add(indicator);
                indicator.SetUp();

                ThreeCandlesInRow[] tmp = new ThreeCandlesInRow[cacheThreeCandlesInRow == null ? 1 : cacheThreeCandlesInRow.Length + 1];
                if (cacheThreeCandlesInRow != null)
                    cacheThreeCandlesInRow.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheThreeCandlesInRow = tmp;
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
        /// Generate a long or short signal and alert whenever three consecutive candles are either all green or red.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.ThreeCandlesInRow ThreeCandlesInRow(string destinationEmailAddress, int requiredConsecutiveBars, bool turnOnEmailAlerts)
        {
            return _indicator.ThreeCandlesInRow(Input, destinationEmailAddress, requiredConsecutiveBars, turnOnEmailAlerts);
        }

        /// <summary>
        /// Generate a long or short signal and alert whenever three consecutive candles are either all green or red.
        /// </summary>
        /// <returns></returns>
        public Indicator.ThreeCandlesInRow ThreeCandlesInRow(Data.IDataSeries input, string destinationEmailAddress, int requiredConsecutiveBars, bool turnOnEmailAlerts)
        {
            return _indicator.ThreeCandlesInRow(input, destinationEmailAddress, requiredConsecutiveBars, turnOnEmailAlerts);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Generate a long or short signal and alert whenever three consecutive candles are either all green or red.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.ThreeCandlesInRow ThreeCandlesInRow(string destinationEmailAddress, int requiredConsecutiveBars, bool turnOnEmailAlerts)
        {
            return _indicator.ThreeCandlesInRow(Input, destinationEmailAddress, requiredConsecutiveBars, turnOnEmailAlerts);
        }

        /// <summary>
        /// Generate a long or short signal and alert whenever three consecutive candles are either all green or red.
        /// </summary>
        /// <returns></returns>
        public Indicator.ThreeCandlesInRow ThreeCandlesInRow(Data.IDataSeries input, string destinationEmailAddress, int requiredConsecutiveBars, bool turnOnEmailAlerts)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.ThreeCandlesInRow(input, destinationEmailAddress, requiredConsecutiveBars, turnOnEmailAlerts);
        }
    }
}
#endregion
