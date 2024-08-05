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
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
    [Gui.CategoryOrder("Oscillator Settings", 01)]
    [Gui.CategoryOrder("CCI Settings", 02)]
    [Gui.CategoryOrder("Z Score Settings", 03)]
    [Gui.CategoryOrder("Trend Magic Settings", 04)]
    [Gui.CategoryOrder("Display Settings", 05)]

    public class TrendMagic : Indicator
	{
        public TrendMagic()
        {
            VendorLicense("DezoAlgoLLC", "TrendMagic", "dezoalgo.com", "dezoalgo@gmail.com");
        }

        private double osc = 0;
		private double atrVal = 0;
		private double upTrend = 0;
		private double downTrend = 0;
		
		private Series<double> Z;
		
		private Series<int> AXIS;
		
		private Series<double> OUTPUT;
		
		private double	bearTick = 0;
		private DateTime bearTickTime = DateTime.MinValue;
		
		private double	bullTick = 0;
		private DateTime bullTickTime = DateTime.MinValue;
		
		//private bool	showMagic = true;
		private bool	scale = false;
		private int		viewTicks = 1;
		private bool	showPrices = true;
		private int		labelPixels = 11;
		private int		textOffsetBars = 0;
		NinjaTrader.Gui.Tools.SimpleFont	textFont = new SimpleFont() { Family = new FontFamily("Courier New"), Size = 13 };
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description						= @"An ATR based trendline regulated by the position of an oscillator.";
				Name							= "Trend Magic";
				IsOverlay						= true; 
				IsSuspendedWhileInactive		= true;
				
				oscType							= "CCI";
				
				averageType						= "SMA";
				priceInput						= "HLC3";
				
				cciPeriod						= 20;
				cciModifier						= 0.015;
				
				averagePeriods					= 100;
				deviationPeriods				= 100;
				
				smoothType						= "SMA";
				smoothBars						= 1;
				
				atrPeriod						= 14;
				atrMult							= 1;
				
				showMagic						= true;
				showArrows						= true;

                useOscTrend						= false;
				bullBrush						= Brushes.Turquoise;
				bearBrush						= Brushes.Violet;
				
				AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Line, Name);
			}
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Second, 1);	//	[1]
			//	-	-	-	-	-	-	-	-	-
				ClearOutputWindow();
			}
			else if (State == State.DataLoaded)
			{
				Trend[0] = 0;
				Z = new Series<double>(BarsArray[0],MaximumBarsLookBack.Infinite);
				AXIS = new Series<int>(BarsArray[0],MaximumBarsLookBack.Infinite);
				OUTPUT = new Series<double>(BarsArray[0],MaximumBarsLookBack.Infinite);
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress == 0){
				
				if (CurrentBar < cciPeriod || CurrentBar < atrPeriod)
					return;
				
				ISeries<double> INPUT = Close;
					
				switch (priceInput){
					case "HL2" : INPUT = Median; break;
					case "HLC3" : INPUT = Typical; break;
					case "OHLC4" : INPUT = Weighted; break;
				}
				
				double average = 0;
				
				switch (oscType){
					case "CCI" :
						switchMA(INPUT,averageType,ref average,cciPeriod);
						double mean = 0;
						for (int idx = Math.Min(CurrentBar, cciPeriod - 1); idx >= 0; idx--)
							mean += Math.Abs(INPUT[idx] - average);
						double cciVal = (INPUT[0] - average) / (mean.ApproxCompare(0) == 0 ? 1 : (cciModifier * (mean / cciPeriod)));
						OUTPUT[0] = cciVal;
						break;
						
					case "Z Score" :
						switchMA(INPUT,averageType,ref average,averagePeriods);
						double tempZ = (INPUT[0] - average) / StdDev(INPUT, deviationPeriods)[0];
						if (Math.Abs(tempZ) > 5)
							tempZ = Z[1];
						Z[0] = tempZ;
						OUTPUT[0] = Z[0];
						break;
				}

				osc = smoothBars > 1 ? switchSmoothMA(smoothType,OUTPUT,smoothBars) : OUTPUT[0];
				
				atrVal = ATR(BarsArray[0],atrPeriod)[0];
				
				upTrend = Low[0] - atrVal * atrMult;
				downTrend = High[0] + atrVal * atrMult;
								
				if (osc >= 0)
					if (upTrend < Trend[1])
						Trend[0] = Trend[1];
					else
						Trend[0] = upTrend;
				else
					if (downTrend > Trend[1])
						Trend[0] = Trend[1];
					else
						Trend[0] = downTrend;
					
				if (useOscTrend)
					AXIS[0] = osc > 0 ? 1 : -1;
				else
					AXIS[0] = Trend[0] > Trend[1] ? 1 : Trend[0] < Trend[1] ? -1 : AXIS[1];
				
				if (crossBelowII(AXIS[1],0,AXIS[0])){
					if (Close[0] < Open[0])
						CandleOutlineBrushes[0] = bearBrush;
					else
						BarBrushes[0] = bearBrush;
					bullTick = 0;
					bearTickTime = DateTime.MinValue;
				}
				
				if (crossAboveII(AXIS[1],0,AXIS[0])){
					if (Close[0] > Open[0])
						CandleOutlineBrushes[0] = bullBrush;
					else
						BarBrushes[0] = bullBrush;
					bearTick = 0;
					bullTickTime = DateTime.MinValue;
				}
				
				PlotBrushes[0][0] = AXIS[0] == 1 ? bullBrush : bearBrush;
				                
                drawLevelWithTextI(showMagic&&bearTick!=0, "↓", " ShortSignal ", bearTick,bearTickTime,Time[0],Brushes.HotPink,textFont,1);
				drawLevelWithTextI(showMagic&&bullTick!=0, "↑", " LongSignal", bullTick,bullTickTime,Time[0],Brushes.Gold,textFont,1);

                if (showArrows && bullTick != 0) Draw.TriangleUp(this, "TrendUp" + CurrentBar.ToString(), true, 0, Low[0] - 10 * TickSize, Brushes.Green);
                if (showArrows && bearTick != 0) Draw.TriangleDown(this, "TrendDown" + CurrentBar.ToString(), true, 0, High[0] + 10 * TickSize, Brushes.Red);
            }
			
			if (BarsInProgress == 1){
				
				if (bearTickTime == DateTime.MinValue){
					bearTick = Close[0];
					bearTickTime = Time[0];
				}
				
				if (bullTickTime == DateTime.MinValue){
					bullTick = Close[0];
					bullTickTime = Time[0];
				}
				
			}
		}
		
		// - - - - - - - - - - - - -
		
//		#region Classes
		
			#region OSC
		
		internal class OSC : StringConverter{
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context){return true;}
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context){return true;}
			public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context){
				return new StandardValuesCollection( new String[] {"CCI", "Z Score"} );
			}
		}
		
			#endregion

			#region MATYPE
		
		internal class MATYPE : StringConverter{
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context){return true;}
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context){return true;}
			public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context){
				return new StandardValuesCollection( new String[] {"DEMA", "EMA", "HMA", "LinReg", "SMA", "SSA", "TEMA", "TMA", "VWMA", "WMA", "ZLEMA"} );
			}
		}
		
			#endregion
		
			#region INPUT
		
		internal class INPUT : StringConverter{
			public override bool GetStandardValuesSupported(ITypeDescriptorContext context){return true;}
			public override bool GetStandardValuesExclusive(ITypeDescriptorContext context){return true;}
			public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context){
				return new StandardValuesCollection( new String[] {"CLOSE", "HL2", "HLC3", "OHLC4"} );
			}
		}
		
			#endregion
		
//		#endregion
		
		// - - - - - - - - - - - - -
		
//		#region Objects
		
			#region Format
		
		private string format (double input)
		{
			return Instrument.MasterInstrument.FormatPrice(input);
		}
		
		private string autoFormat()
		{
			string s = TickSize.ToString();
			for (int i = 1; i <= 9; i++){
					s = s.Replace(i.ToString(), "0");	
			}
			return s;
		}
		
			#endregion
		
			#region Crosses
		
		private bool crossAboveII (double initialValue,double focusValue,double endValue)
		{	
			return initialValue <= focusValue && endValue > focusValue;
		}
		
		private bool crossBelowII (double initialValue,double focusValue,double endValue)
		{	
			return initialValue >= focusValue && endValue < focusValue;
		}
		
		private bool priceAction (double iV,double fV,double eV)
		{
			return crossBelowII(iV,fV,eV) || crossAboveII(iV,fV,eV);
		}
		
			#endregion
		
			#region Switch MA
				
		private void switchMA (ISeries<double> input,string type,ref double series,int periods)
		{
			switch (type)
			{
				case "DEMA"		:	series = DEMA(input,periods)[0];	break;
				case "EMA"		:	series = EMA(input,periods)[0];		break;
				case "HMA"		:	series = HMA(input,periods)[0];		break;
				case "LinReg"	:	series = LinReg(input,periods)[0];	break;
				case "SMA"		:	series = SMA(input,periods)[0];		break;
				case "SSA"		:	series = SSA(input,periods)[0];		break;
				case "TEMA"		:	series = TEMA(input,periods)[0];	break;
				case "TMA"		:	series = TMA(input,periods)[0];		break;
				case "VWMA"		:	series = VWMA(input,periods)[0];	break;
				case "WMA"		:	series = WMA(input,periods)[0];		break;
				case "ZLEMA"	:	series = ZLEMA(input,periods)[0];	break;
			}	
		}
		
		private double switchSmoothMA (string type,Series<double> input,int periods)
		{
			double output = 0;
			switch (type)
			{
				case "DEMA"		:	output = DEMA(input,periods)[0];	break;
				case "EMA"		:	output = EMA(input,periods)[0];		break;
				case "HMA"		:	output = HMA(input,periods)[0];		break;
				case "LinReg"	:	output = LinReg(input,periods)[0];	break;
				case "SMA"		:	output = SMA(input,periods)[0];		break;
				case "SSA"		:	output = SSA(input,periods)[0];		break;
				case "TEMA"		:	output = TEMA(input,periods)[0];	break;
				case "TMA"		:	output = TMA(input,periods)[0];		break;
				case "VWMA"		:	output = VWMA(input,periods)[0];	break;
				case "WMA"		:	output = WMA(input,periods)[0];		break;
				case "ZLEMA"	:	output = ZLEMA(input,periods)[0];	break;
			}
			return output;
		}

			#endregion
		
			#region Draw Level
		
		private void drawLevelWithTextI (bool show,string text,string label,double price,
											DateTime referenceTime,DateTime thisBar,
											Brush color,SimpleFont font,int lineThickness)
		{
			if (show)
			if (referenceTime != DateTime.MinValue){
				int barsAgo = CurrentBar-Bars.GetBar(referenceTime);
				int barsEnd = CurrentBar-Bars.GetBar(thisBar);
				string lineTag = label+referenceTime;
				string wordTag = label+" "+referenceTime;
				int position = Close[0]>price+viewTicks*TickSize?-labelPixels:labelPixels;
				Draw.Line(this,lineTag,scale,barsAgo,price,-1,price,color,DashStyleHelper.Dash,lineThickness);
				string textInput = text+" "+(showPrices?price.ToString(autoFormat()):"");
				Draw.Text(this,wordTag,scale,textInput,barsEnd+textOffsetBars,price,position,color,font,TextAlignment.Right,Brushes.Transparent,Brushes.Transparent,0);
			}
		}
		
			#endregion
		
//		#endregion
		
		// - - - - - - - - - - - - -

		#region Properties
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Trend
		{
			get { return Values[0]; }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Oscillator Type", GroupName = "Oscillator Settings", Order = 1)]
        [TypeConverter(typeof(OSC))]
		public string oscType { get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Moving Average Type", GroupName = "Oscillator Settings", Order = 2)]
        [TypeConverter(typeof(MATYPE))]
		public string averageType { get; set; }
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Source", GroupName = "Oscillator Settings", Order = 3)]
        [TypeConverter(typeof(INPUT))]
		public string priceInput { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "CCI Period", GroupName = "CCI Settings", Order = 1)]
        public int cciPeriod { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "CCI Modifier", GroupName = "CCI Settings", Order = 2)]
        public double cciModifier { get; set; }
		
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Moving Average Period", GroupName = "Z Score Settings", Order = 1)]
        public int averagePeriods { get; set; }
		
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Deviation Period", GroupName = "Z Score Settings", Order = 2)]
        public int deviationPeriods { get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smoothing Type", GroupName = "Trend Magic Settings", Order = 1)]
		[TypeConverter(typeof(MATYPE))]
		public string smoothType { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Smooth Bars", GroupName = "Trend Magic Settings", Order = 2)]
        public int smoothBars { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ATR Period", GroupName = "Trend Magic Settings", Order = 3)]
        public int atrPeriod { get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "ATR Multiplier", GroupName = "Trend Magic Settings", Order = 4)]
        public double atrMult { get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Use Oscillator Trend", GroupName = "Trend Magic Settings", Order = 5)]
		public bool useOscTrend { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Show Trend Price Lines", GroupName = "Display Settings", Order = 1)]
        public bool showMagic { get; set; }

        [NinjaScriptProperty]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Show Trend Arrows", GroupName = "Display Settings", Order = 2)]
        public bool showArrows { get; set; }

        [XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Bull Line Color", GroupName = "Display Settings", Order = 3)]
        public Brush bullBrush { get; set; }
        [Browsable(false)]
        public string bullBrushS{
			get { return Serialize.BrushToString(bullBrush); }
  			set { bullBrush = Serialize.StringToBrush(value); }
        }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Bear Line Color", GroupName = "Display Settings", Order = 4)]
        public Brush bearBrush { get; set; }
        [Browsable(false)]
        public string bearBrushS{
			get { return Serialize.BrushToString(bearBrush); }
  			set { bearBrush = Serialize.StringToBrush(value); }
        }

		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TrendMagic[] cacheTrendMagic;
		public TrendMagic TrendMagic(int cciPeriod, double cciModifier, int averagePeriods, int deviationPeriods, string smoothType, int smoothBars, int atrPeriod, double atrMult, bool useOscTrend, bool showMagic, bool showArrows)
		{
			return TrendMagic(Input, cciPeriod, cciModifier, averagePeriods, deviationPeriods, smoothType, smoothBars, atrPeriod, atrMult, useOscTrend, showMagic, showArrows);
		}

		public TrendMagic TrendMagic(ISeries<double> input, int cciPeriod, double cciModifier, int averagePeriods, int deviationPeriods, string smoothType, int smoothBars, int atrPeriod, double atrMult, bool useOscTrend, bool showMagic, bool showArrows)
		{
			if (cacheTrendMagic != null)
				for (int idx = 0; idx < cacheTrendMagic.Length; idx++)
					if (cacheTrendMagic[idx] != null && cacheTrendMagic[idx].cciPeriod == cciPeriod && cacheTrendMagic[idx].cciModifier == cciModifier && cacheTrendMagic[idx].averagePeriods == averagePeriods && cacheTrendMagic[idx].deviationPeriods == deviationPeriods && cacheTrendMagic[idx].smoothType == smoothType && cacheTrendMagic[idx].smoothBars == smoothBars && cacheTrendMagic[idx].atrPeriod == atrPeriod && cacheTrendMagic[idx].atrMult == atrMult && cacheTrendMagic[idx].useOscTrend == useOscTrend && cacheTrendMagic[idx].showMagic == showMagic && cacheTrendMagic[idx].showArrows == showArrows && cacheTrendMagic[idx].EqualsInput(input))
						return cacheTrendMagic[idx];
			return CacheIndicator<TrendMagic>(new TrendMagic(){ cciPeriod = cciPeriod, cciModifier = cciModifier, averagePeriods = averagePeriods, deviationPeriods = deviationPeriods, smoothType = smoothType, smoothBars = smoothBars, atrPeriod = atrPeriod, atrMult = atrMult, useOscTrend = useOscTrend, showMagic = showMagic, showArrows = showArrows }, input, ref cacheTrendMagic);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TrendMagic TrendMagic(int cciPeriod, double cciModifier, int averagePeriods, int deviationPeriods, string smoothType, int smoothBars, int atrPeriod, double atrMult, bool useOscTrend, bool showMagic, bool showArrows)
		{
			return indicator.TrendMagic(Input, cciPeriod, cciModifier, averagePeriods, deviationPeriods, smoothType, smoothBars, atrPeriod, atrMult, useOscTrend, showMagic, showArrows);
		}

		public Indicators.TrendMagic TrendMagic(ISeries<double> input , int cciPeriod, double cciModifier, int averagePeriods, int deviationPeriods, string smoothType, int smoothBars, int atrPeriod, double atrMult, bool useOscTrend, bool showMagic, bool showArrows)
		{
			return indicator.TrendMagic(input, cciPeriod, cciModifier, averagePeriods, deviationPeriods, smoothType, smoothBars, atrPeriod, atrMult, useOscTrend, showMagic, showArrows);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TrendMagic TrendMagic(int cciPeriod, double cciModifier, int averagePeriods, int deviationPeriods, string smoothType, int smoothBars, int atrPeriod, double atrMult, bool useOscTrend, bool showMagic, bool showArrows)
		{
			return indicator.TrendMagic(Input, cciPeriod, cciModifier, averagePeriods, deviationPeriods, smoothType, smoothBars, atrPeriod, atrMult, useOscTrend, showMagic, showArrows);
		}

		public Indicators.TrendMagic TrendMagic(ISeries<double> input , int cciPeriod, double cciModifier, int averagePeriods, int deviationPeriods, string smoothType, int smoothBars, int atrPeriod, double atrMult, bool useOscTrend, bool showMagic, bool showArrows)
		{
			return indicator.TrendMagic(input, cciPeriod, cciModifier, averagePeriods, deviationPeriods, smoothType, smoothBars, atrPeriod, atrMult, useOscTrend, showMagic, showArrows);
		}
	}
}

#endregion
