#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX.DirectWrite;
using SharpDX;
using SharpDX.Direct2D1;
using System.Linq; 
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class OrderLadder : Indicator
	{
		#region Drawing Variables

		private TextFormat rungTextFormat;
		/// <summary>
		/// Default brush used to draw text
		/// </summary>
		private System.Windows.Media.Brush rungTextBrush;
		/// <summary>
		/// Brush used to draw green rungs 
		/// </summary>
		private System.Windows.Media.Brush rungGreenRectBrush;
		/// <summary>
		/// Brush used to draw red rungs 
		/// </summary>
		private System.Windows.Media.Brush rungRedRectBrush;
		/// <summary>
		/// Brush used to draw red text 
		/// </summary>
		private System.Windows.Media.Brush rungRedTextBrush;
		/// <summary>
		/// Brush used to draw green text 
		/// </summary>
		private System.Windows.Media.Brush rungGreenTextBrush;
		/// <summary>
		/// Brush used to draw the buy bars in the volume profile
		/// </summary>
		private System.Windows.Media.Brush volumeBuyBrush;
		/// <summary>
		/// Brush used to draw the sell bars in the volume profile
		/// </summary>
		private System.Windows.Media.Brush volumeSellBrush;
		
		/// <summary>
		/// Brush used to draw rung rectangle outlines 
		/// </summary>
		private System.Windows.Media.Brush rungOutlineBrush; 
		/// <summary>
		/// Cached SharpDX.Direct2D1.Brush used to draw outlines of price rungs
		/// </summary>
		private SharpDX.Direct2D1.Brush rungOutlineDirectBrush;
		/// <summary>
		/// Cached SharpDX.Direct2D1.Brush used to draw buy bars in volume profile 
		/// </summary>
		private SharpDX.Direct2D1.Brush volumeBuyDirectBrush;  
		/// <summary>
		/// Cached SharpDX.Direct2D1.Brush used to draw sell bars in volume profile 
		/// </summary>
		private SharpDX.Direct2D1.Brush volumeSellDirectBrush;  
		/// <summary>
		/// The font size, in pt, of all text rendered by the indicator. 
		/// </summary>
		private float textSize; 
		/// <summary>
		/// Font family of font
		/// </summary>
		private string fontFamily; 	

		#endregion

		#region Volume Profile Variables 
		/// <summary>
		/// Opacity value of the volume profile: value from 0-1, where 0 = transparent, 1 = opaque
		/// </summary>
		private double volumeProfileOpacity;
		/// <summary>
		/// The max horizontal width (in pixels) of the volume profile
		/// </summary>
		private double volumeProfileWidth; 
		/// <summary>
		/// Show the volume profile?
		/// </summary>
		private bool showVolumeProfile; 
		/// <summary>
		/// Possible locations of the volume profile, relative to the plotted bars.
		/// </summary>
		public enum VolumeProfileLocation { In_Front, Behind_Bars }; 
		/// <summary>
		/// Show the volume profile behind or in front of the bars?
		/// </summary>
		private VolumeProfileLocation volumeProfileLocation; 

		#endregion 

		#region Data Structures

		/// <summary>
		/// Buy and sell data for a given price
		/// </summary>
		public class Rung {
			private long buy;
			private long sell;
			public Rung(long volume, bool isBuy) 
			{
				buy = isBuy ? volume : 0;
				sell = isBuy ? volume : 0; 
			}
			public long Buy { 
				get { return buy; }  
				set { buy = value; } 
			}
			public long Sell { 
				get { return sell; }  
				set { sell = value; } 
			}
			public override string ToString() 
			{
				return "Buys: " + this.buy + " Sells: " + sell; 	
			}
		}

		/// <summary>
		/// Buy, sell, and delta values for a given bar
		/// </summary>
		public class BarVolume { 
			private long buys; 
			private long sells; 
			public long maxdelta;
			public long mindelta;
			public Dictionary<double, Rung> BuySellLadder; 
			public double pivot; 
			public long lastDelta; 

			public BarVolume(long _buys, long _sells, long _lastDelta) 
			{
				buys = _buys;
				sells = _sells;
				maxdelta = -1<<28;
				mindelta = 1<<28; 
				pivot = -1d;
				lastDelta = _lastDelta; 
				BuySellLadder = new Dictionary<double, Rung>(); 
			}
			
			public void AddOrder(long volume, bool isBuy, double price) {
				if (isBuy) buys = buys + volume;
				else sells = sells + volume;

				if (!BuySellLadder.ContainsKey(price))
					BuySellLadder.Add(price, new Rung(0, false)); 
				if (isBuy) BuySellLadder[price].Buy += volume;
				else BuySellLadder[price].Sell += volume; 

				if (pivot == -1d || 
					BuySellLadder[price].Buy + BuySellLadder[price].Sell > 
					BuySellLadder[pivot].Buy + BuySellLadder[pivot].Sell)
					pivot = price; 
				
				maxdelta = Math.Max(maxdelta, Difference); 
				mindelta = Math.Min(mindelta, Difference); 
			}
			
			public long Difference {
				get {
					return this.BuySellLadder.Sum(x => x.Value.Buy - x.Value.Sell); 
				}
			}
			
			public bool isSellingSignificant(double price, double tickSize, double ratio) { 
				if (!BuySellLadder.ContainsKey(price) || !BuySellLadder.ContainsKey(price + tickSize)) return false; 
				else if (BuySellLadder[price+tickSize].Buy == 0) return false;
				else return BuySellLadder[price].Sell / BuySellLadder[price+tickSize].Buy >= ratio; 
			}
			
			public bool isBuyingSignificant(double price, double tickSize, double ratio) { 
				if (!BuySellLadder.ContainsKey(price) || !BuySellLadder.ContainsKey(price - tickSize)) return false; 
				else if (BuySellLadder[price-tickSize].Sell == 0) return false;
				else return BuySellLadder[price].Buy / BuySellLadder[price-tickSize].Sell >= ratio; 
			} 
			
			public long CumulativeDelta { 
				get { return this.Difference + this.lastDelta; }	
			}
		}
		#endregion  
		
		/// <summary>
		/// Order flow data
		/// </summary> 
		protected Dictionary<int, BarVolume> OrderFlowData;

		/// <summary>
		/// Volume profile data
		/// </summary>
		Dictionary<double, Rung> VolumeProfileData; 

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Custom order flow indicator for NinjaTrader 8. Charts the buys and sells at each price level for each bar in the form of a ladder, and also provides a volume profile.";
				Name										= "OrderLadder_NT8 v0.1";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				BarsRequiredToPlot							= 5; 
				
				
				this.textSize = 10f; 
				this.fontFamily = "Arial"; 
				this.rungTextBrush = System.Windows.Media.Brushes.White;
				this.rungRedTextBrush = System.Windows.Media.Brushes.Red;
				this.rungGreenTextBrush = System.Windows.Media.Brushes.Blue;

				this.rungRedRectBrush = System.Windows.Media.Brushes.Red;
				this.rungGreenRectBrush = System.Windows.Media.Brushes.Blue;
				this.rungOutlineBrush = System.Windows.Media.Brushes.DarkGray; 
				this.rungOutlineDirectBrush = null;

				this.volumeBuyBrush = System.Windows.Media.Brushes.Blue; 
				this.volumeBuyDirectBrush = null;
				this.volumeSellBrush = System.Windows.Media.Brushes.Red; 
				this.volumeSellDirectBrush = null; 
				this.volumeProfileOpacity = 0.25d; 
				this.volumeProfileWidth = 500d; 

				this.showVolumeProfile = true;
				this.volumeProfileLocation = VolumeProfileLocation.In_Front;
				

			}
			else if (State == State.Configure)
			{
				AddDataSeries(this.Instrument.FullName, BarsPeriodType.Tick, 1, MarketDataType.Last); // BarsInProgress == 1
				this.rungTextFormat = new TextFormat(NinjaTrader.Core.Globals.DirectWriteFactory, this.fontFamily, this.textSize);
			}
			else if (State == State.Terminated)
			{
				if (this.rungTextFormat != null && !this.rungTextFormat.IsDisposed) this.rungTextFormat.Dispose(); 
				DisposeBrushOnTerminate(this.rungOutlineDirectBrush);
				DisposeBrushOnTerminate(this.volumeBuyDirectBrush);
				DisposeBrushOnTerminate(this.volumeSellDirectBrush);
			}
		}

		/// <summary>
		/// Dispose a given SharpDX.Direct2D1.Brush
		/// </summary>
		/// <param name="_brush"></param>
		private void DisposeBrushOnTerminate(SharpDX.Direct2D1.Brush _brush)
		{
			if (_brush != null && !_brush.IsDisposed) 
			{
				_brush.Dispose(); 
				_brush = null;
			} 
		}

		/// <summary>
		/// Cached reference to the current bar being added to when indicator is calculating OnMarketData
		/// </summary>
		private BarVolume currentBar; 

		/// <summary>
		/// Perform calculations every tick 
		/// </summary>
		protected override void OnBarUpdate()
		{	
			if (CurrentBars[0] < this.BarsRequiredToPlot) return; 
			if (OrderFlowData == null) 
			{
				OrderFlowData = new Dictionary<int, BarVolume>(); 
				currentBar = new BarVolume(0, 0, 0);
				OrderFlowData.Add(CurrentBars[0], currentBar); 
			}
			try {
				if (State == State.Historical)
				{
					if (!OrderFlowData.ContainsKey(CurrentBars[0]+1)) 
					{
				    	currentBar = new BarVolume(0, 0, 0);
				    	OrderFlowData.Add(CurrentBars[0]+1, currentBar); 
					} 
				}
				else if (State == State.Realtime)
				{
					if (BarsInProgress == 0 && this.IsFirstTickOfBar && !OrderFlowData.ContainsKey(CurrentBars[0])) {
				    	currentBar = new BarVolume(0, 0, 0);
				    	OrderFlowData.Add(CurrentBars[0], currentBar); 
					}
				}
			}
			catch 
			{
				Print("error in checking time[0] of bar #" + CurrentBars[0]);
			}

			if (State == State.Historical && BarsInProgress == 1 && CurrentBars[0] > 2)
			{
				double ask = BarsArray[1].GetAsk(CurrentBars[1]);
				double bid = BarsArray[1].GetBid(CurrentBars[1]);
				double last = BarsArray[1].GetClose(CurrentBars[1]);
				long vol = BarsArray[1].GetVolume(CurrentBars[1]);

				// https://ninjatrader.com/support/forum/forum/ninjatrader-8/indicator-development/1132473-historical-bid-ask-and-cumulative-delta
				if (last >= ask) 		currentBar.AddOrder(vol, true, ask);
				else if (last <= bid) 	currentBar.AddOrder(vol, false, bid);
			} 
		}

		protected override void OnMarketData(MarketDataEventArgs marketOrder)
		{
			//Print(String.Format("Found market data at time {0}, last {1}, bid {2}, ask {3} ", marketOrder.Time.ToString(), marketOrder.Last, marketOrder.Bid, marketOrder.Ask)); 
			if (State == State.Realtime && BarsInProgress == 1 && marketOrder.MarketDataType == MarketDataType.Last)
			{
				double price = marketOrder.Price;
				if (price >= marketOrder.Ask)
					currentBar.AddOrder(marketOrder.Volume, true, marketOrder.Ask);
				else if (price <= marketOrder.Bid)
					currentBar.AddOrder(marketOrder.Volume, false, marketOrder.Bid);
			}
		}
		
		#region Drawing Tools 

		// private void DrawWick(int barIdx, double _high, double _low, ChartControl chartControl, ChartScale chartScale)
		// {
		// 	if (this.wickDirectBrush != null) this.wickDirectBrush = this.wickBrush.ToDxBrush(RenderTarget);
		// 	float centreXPixel = chartControl.GetXByBarIndex(ChartBars, barIdx); 
		// 	float highYPixel = chartScale.GetYByValue(_high); 
		// 	float lowYPixel = chartScale.GetYByValue(_low); 
		// 	RectangleF rect = new RectangleF(centreXPixel - this.wickWidth / 2f, highYPixel, this.wickWidth, lowYPixel - highYPixel);
		// 	RenderTarget.FillRectangle(rect, wickDirectBrush); 
		// 	wickDirectBrush.Dispose(); 
		// }

		/// <summary>
		/// Draw a price box at a given price level and bar index.
		/// </summary>
		/// <param name="barIdx">Bar index, as based on CurrentBar</param>
		/// <param name="priceLevel">Price level to be drawn at</param>
		/// <param name="chartControl">Current chartControl supplied to OnRender</param>
		/// <param name="chartScale">Current chartScale supplied to OnRender</param>
		/// <param name="systemBrush">Brush used to draw the box</param>
		/// <param name="isFilled">Fill the box with the same color?</param>
		private void WriteBoxAtPrice(int barIdx, double priceLevel, ChartControl chartControl, ChartScale chartScale, System.Windows.Media.Brush systemBrush, bool isFilled = true)
		{
			float centreXPixel = chartControl.GetXByBarIndex(ChartBars, barIdx); 
			float rungWidth = (float)chartControl.BarWidth; 

			float belowYPixel = chartScale.GetYByValue(priceLevel - TickSize); 
			float centreYPixel = chartScale.GetYByValue(priceLevel); 
			float aboveYPixel = chartScale.GetYByValue(priceLevel + TickSize); 
			float rungHeight = belowYPixel - centreYPixel; 

			RectangleF rect = new RectangleF(centreXPixel - rungWidth - 2f, (aboveYPixel + centreYPixel) / 2f, rungWidth * 2 + 4f, rungHeight); 
			
			SharpDX.Direct2D1.Brush directRectBrush = systemBrush.ToDxBrush(RenderTarget);
			if (isFilled)
				RenderTarget.FillRectangle(rect, directRectBrush);
			else
				RenderTarget.DrawRectangle(rect, directRectBrush);

			if (rungOutlineDirectBrush == null)
				rungOutlineDirectBrush = this.rungOutlineBrush.ToDxBrush(RenderTarget);
			RenderTarget.DrawRectangle(rect, this.rungOutlineDirectBrush); 
			directRectBrush.Dispose(); 
		}
		
		/// <summary>
		/// Write text at a given location 
		/// </summary>
		/// <param name="barIdx">Bar index, as based on CurrentBar</param>
		/// <param name="priceLevel">Price level to be drawn at</param>
		/// <param name="text">The text string to be written.</param>
		/// <param name="chartControl">Current chartControl supplied to OnRender</param>
		/// <param name="chartScale">Current chartScale supplied to OnRender</param>
		/// <param name="textAlignment">Text alignment of the text within bar</param>
		/// <param name="systemBrush">Brush used to write the text</param>
		private void WriteTextAtPrice(int barIdx, double priceLevel, string text, ChartControl chartControl, ChartScale chartScale, SharpDX.DirectWrite.TextAlignment textAlignment, System.Windows.Media.Brush systemBrush)
		{
			float margin = 2f;
			float centreXPixel = chartControl.GetXByBarIndex(ChartBars, barIdx); 
			float rungWidth = (float)chartControl.BarWidth; 

			float belowYPixel = chartScale.GetYByValue(priceLevel - TickSize); 
			float centreYPixel = chartScale.GetYByValue(priceLevel); 
			float rungHeight = belowYPixel - centreYPixel; 

			RectangleF rect = new RectangleF(centreXPixel - rungWidth + margin, centreYPixel - textSize / 2f, rungWidth * 2f - margin * 2f, rungHeight); 
			SharpDX.Direct2D1.Brush directTextBrush = systemBrush.ToDxBrush(RenderTarget);
			rungTextFormat.TextAlignment = textAlignment; 

			RenderTarget.DrawText(text, this.rungTextFormat, rect, directTextBrush); 
			directTextBrush.Dispose();
		} 

		/// <summary>
		/// Draw the volume profile bars for a given price level
		/// </summary>
		/// <param name="priceLevel">Price level to be drawn at</param>
		/// <param name="buy">Number of buys at this price level</param>
		/// <param name="sell">Number of sells at this price level</param>
		/// <param name="maxVolume">The volume that would give a bar the max width for the volume profile</param>
		/// <param name="chartControl">Current chartControl supplied to OnRender</param>
		/// <param name="chartScale">Current chartScale supplied to OnRender</param>
		private void WriteVolumeProfileAtPrice(double priceLevel, long buy, long sell, long maxVolume, ChartControl chartControl, ChartScale chartScale)
		{
			// Print(String.Format("Drawing profile for price {0}, buys {1}, sells {2}", priceLevel, buy, sell)); 
			float rungWidth = (float)chartControl.BarWidth; 

			float centreYPixel = chartScale.GetYByValue(priceLevel); 
			float aboveYPixel = chartScale.GetYByValue(priceLevel + TickSize); 
			float rungHeight = centreYPixel - aboveYPixel; 

			// Draw stacked bars (buy first, sell second)
			if (this.volumeSellDirectBrush == null)
			{
				this.volumeSellDirectBrush = this.volumeSellBrush.ToDxBrush(this.RenderTarget); 
				this.volumeSellDirectBrush.Opacity = (float)this.VolumeProfileOpacity; 
			} 
			if (this.volumeBuyDirectBrush == null) 
			{
				this.volumeBuyDirectBrush = this.volumeBuyBrush.ToDxBrush(this.RenderTarget); 
				this.volumeBuyDirectBrush.Opacity = (float)this.VolumeProfileOpacity; 
			}
			int buyWidth = (int)((double)buy / (double)maxVolume * this.volumeProfileWidth); 
			int sellWidth = (int)((double)sell / (double)maxVolume * this.volumeProfileWidth);
            rungHeight = (float)Math.Round(rungHeight, 0); 
			// Draw sell bar on left 
			RectangleF sellRect = new RectangleF(0, (aboveYPixel + centreYPixel) / 2f, sellWidth, rungHeight); 
			RenderTarget.FillRectangle(sellRect, this.volumeSellDirectBrush);
            // Draw buy bar on right
            RectangleF buyRect = new RectangleF(sellWidth, (aboveYPixel + centreYPixel) / 2f, buyWidth, rungHeight); 
			RenderTarget.FillRectangle(buyRect, this.volumeBuyDirectBrush);
		}

		#endregion 
		
		/// <summary>
		/// Refresh the indicator on the chart
		/// </summary>
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            base.OnRender(chartControl, chartScale);
            if (OrderFlowData == null)
                return;

            this.VolumeProfileData = new Dictionary<double, Rung>();

			if (this.volumeProfileLocation == VolumeProfileLocation.Behind_Bars)
			{
				if (this.showVolumeProfile)
            		DrawVolumeProfile(chartControl, chartScale);
            	DrawOrderFlow(chartControl, chartScale);
			}
			else if (this.volumeProfileLocation == VolumeProfileLocation.In_Front)
			{
            	DrawOrderFlow(chartControl, chartScale);
				if (this.showVolumeProfile)
            		DrawVolumeProfile(chartControl, chartScale);
			}
        }

		/// <summary>
		/// Calculate the buy and sell data for the volume profile
		/// </summary>
        private void CalculateVolumeProfile()
        {
            // Aggregate data for the volume profile
            for (int i = this.ChartBars.FromIndex; i <= this.ChartBars.ToIndex; i++)
            {
                if (i < 10) continue;
                if (OrderFlowData != null && OrderFlowData.ContainsKey(i))
                {
                    foreach (KeyValuePair<double, Rung> value in this.OrderFlowData[i].BuySellLadder)
                    {
                        double price = value.Key;
                        if (!VolumeProfileData.ContainsKey(price))
                            VolumeProfileData.Add(price, new Rung(0, false));
                        Rung priceLevel = VolumeProfileData[price];
                        priceLevel.Buy += value.Value.Buy;
                        priceLevel.Sell += value.Value.Sell;
                    }
                }
            }
        }

		/// <summary>
		/// Draw the entire volume profile
		/// </summary>
		/// <param name="chartControl">Current chartControl supplied to OnRender</param>
		/// <param name="chartScale">Current chartScale supplied to OnRender</param>
        protected void DrawVolumeProfile(ChartControl chartControl, ChartScale chartScale)
        {
			CalculateVolumeProfile();
            #region Draw Volume Profile
            // Calculate the max volume of the prices visible 
			long maxVolumeVisible = 0; 
			if (this.VolumeProfileData.Count > 0)
            	maxVolumeVisible = this.VolumeProfileData.Max(x => x.Value.Sell + x.Value.Buy);
			if (maxVolumeVisible > 0)
			{
				foreach (KeyValuePair<double, Rung> volumeBar in this.VolumeProfileData)
				{
					this.WriteVolumeProfileAtPrice(volumeBar.Key, volumeBar.Value.Buy, volumeBar.Value.Sell, maxVolumeVisible, chartControl, chartScale);
				}
			}
            #endregion
        }

		/// <summary>
		/// Draw the data bars
		/// </summary>
		/// <param name="chartControl">Current chartControl supplied to OnRender</param>
		/// <param name="chartScale">Current chartScale supplied to OnRender</param>
        protected void DrawOrderFlow(ChartControl chartControl, ChartScale chartScale)
        {
            for (int i = this.ChartBars.FromIndex; i <= this.ChartBars.ToIndex; i++)
            {
                try
                {
                    if (i < this.BarsRequiredToPlot) continue;
                    // Obtain bar data of the primary series (the minute chart)
                    double _close = 1d, _open = 1d, _high = 1d, _low = 1d;
                    try
                    {
                        _close = Closes[0].GetValueAt(i);
                        _open = Opens[0].GetValueAt(i);
                        _high = Highs[0].GetValueAt(i);
                        _low = Lows[0].GetValueAt(i);
                    }
                    catch
                    {
                        Print("Caught error while retrieving data on bar index " + i);
                        continue;
                    }
                    // Draw bars to replace candles
                    double candleBottom = Math.Min(_open, _close);
                    double candleTop = Math.Max(_open, _close);
                    for (double price = candleBottom; price <= candleTop; price += TickSize)
                    {
                        this.WriteBoxAtPrice(i, price, chartControl, chartScale, _close > _open ? this.rungGreenRectBrush : this.rungRedRectBrush, true);
                    }

                    if (OrderFlowData != null && OrderFlowData.ContainsKey(i))
                    {
                        //Print("Market data found for " + _time.ToString()); 
                        foreach (KeyValuePair<double, Rung> value in this.OrderFlowData[i].BuySellLadder)
                        {
                            double price = value.Key;

                            // Draw the sell on the left, buy on the right
                            this.WriteTextAtPrice(i, price, value.Value.Sell.ToString("N0"), chartControl, chartScale, SharpDX.DirectWrite.TextAlignment.Leading, this.rungTextBrush);
                            this.WriteTextAtPrice(i, price, value.Value.Buy.ToString("N0"), chartControl, chartScale, SharpDX.DirectWrite.TextAlignment.Trailing, this.rungTextBrush);
                        }
                        // Write the delta above the wick  
                        this.WriteTextAtPrice(i, _high + 2d * TickSize, this.OrderFlowData[i].Difference.ToString("N0"), chartControl, chartScale, SharpDX.DirectWrite.TextAlignment.Center, this.OrderFlowData[i].Difference > 0 ? this.rungGreenTextBrush : this.rungRedTextBrush);
                    }
                    else
                    {
                        Print("No market data for bar index #" + i);
                    }
                }
                catch
                {
                    Print("Caught error at bar index " + i);
                }

            }
        }

        #region Visual Parameters 

        [Display(ResourceType = typeof(Custom.Resource), Name = "Default Font Color", GroupName = "Color/Text Parameters", Order = 0, Description = "Default font color used to draw text")]
        public System.Windows.Media.Brush RungTextBrush
        {
            get { return rungTextBrush; }
            set { rungTextBrush = value; }
        } 

		[Display(ResourceType = typeof(Custom.Resource), Name = "Up Bar Color", GroupName = "Color/Text Parameters", Order = 2, Description = "Color used to draw up bars")]
        public System.Windows.Media.Brush RungGreenRectBrush
        {
            get { return rungGreenRectBrush; }
            set { rungGreenRectBrush = value; }
        } 

		[Display(ResourceType = typeof(Custom.Resource), Name = "Down Bar Color", GroupName = "Color/Text Parameters", Order = 2, Description = "Color used to draw down bars")]
        public System.Windows.Media.Brush RungRedRectBrush
        {
            get { return rungRedRectBrush; }
            set { rungRedRectBrush = value; }
        } 

		[Display(ResourceType = typeof(Custom.Resource), Name = "Negative Text Color", GroupName = "Color/Text Parameters", Order = 0, Description = "Font color used to draw \"negative\" text")]
        public System.Windows.Media.Brush RungRedTextBrush
        {
            get { return rungRedTextBrush; }
            set { rungRedTextBrush = value; }
        } 

		[Display(ResourceType = typeof(Custom.Resource), Name = "Positive Text Color", GroupName = "Color/Text Parameters", Order = 0, Description = "Font color used to draw \"positive\" text")]
        public System.Windows.Media.Brush RungGreenTextBrush
        {
            get { return rungGreenTextBrush; }
            set { rungGreenTextBrush = value; }
        } 

		[Range(1f, 30f)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Font Size", GroupName = "Color/Text Parameters", Order = 0, Description = "Font size used to write all text.")]
        public float TextSize
        {
            get { return textSize; }
            set { textSize = value; }
        } 

		[Range(1d, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Volume Profile Width", GroupName = "Color/Text Parameters", Order = 0, Description = "Maximum width of the volume profile.")]
        public double VolumeProfileWidth
        {
            get { return this.volumeProfileWidth; }
            set { this.volumeProfileWidth = value; }
        } 

		[Range(0d, 1d)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Volume Profile Opacity", GroupName = "Color/Text Parameters", Order = 0, Description = "Opacity of the volume profile: 0 = transparent, 1 = opaque.")]
        public double VolumeProfileOpacity
        {
            get { return this.volumeProfileOpacity; }
            set { this.volumeProfileOpacity = value; }
        } 

		[Display(ResourceType = typeof(Custom.Resource), Name = "Show Volume Profile?", GroupName = "Display Options", Order = 0, Description = "Toggle whether the volume profile is visible.")]
		public bool ShowVolumeProfile
		{
			get { return this.showVolumeProfile; }
			set { this.showVolumeProfile = value; }
		}

		[Display(ResourceType = typeof(Custom.Resource), Name = "Volume Profile Location", GroupName = "Display Options", Order = 0, Description = "Show volume profile in front of or behind the chart bars?")]
		public VolumeProfileLocation Volume_ProfileLocation
		{
			get { return this.volumeProfileLocation; }
			set { this.volumeProfileLocation = value; }
		}
		#endregion


	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private OrderLadder[] cacheOrderLadder;
		public OrderLadder OrderLadder()
		{
			return OrderLadder(Input);
		}

		public OrderLadder OrderLadder(ISeries<double> input)
		{
			if (cacheOrderLadder != null)
				for (int idx = 0; idx < cacheOrderLadder.Length; idx++)
					if (cacheOrderLadder[idx] != null &&  cacheOrderLadder[idx].EqualsInput(input))
						return cacheOrderLadder[idx];
			return CacheIndicator<OrderLadder>(new OrderLadder(), input, ref cacheOrderLadder);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.OrderLadder OrderLadder()
		{
			return indicator.OrderLadder(Input);
		}

		public Indicators.OrderLadder OrderLadder(ISeries<double> input )
		{
			return indicator.OrderLadder(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.OrderLadder OrderLadder()
		{
			return indicator.OrderLadder(Input);
		}

		public Indicators.OrderLadder OrderLadder(ISeries<double> input )
		{
			return indicator.OrderLadder(input);
		}
	}
}

#endregion

OrderLadder_NT8.cs
Displaying OrderLadder_NT8.cs.