//
// Copyright (C) 2021, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// This namespace holds indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators
{
	/// <summary>
	/// The SSA (Simple Smoothed Average) is an indicator that shows the smoothed average value of a security's price over a period of time.
	/// </summary>
	public class SSA : Indicator
	{
		private double priorSum;
		private double sum;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= NinjaTrader.Custom.Resource.NinjaScriptIndicatorDescriptionSMA;
				Name						= "SSA";
				IsOverlay					= true;
				IsSuspendedWhileInactive	= true;
				Period						= 14;

				AddPlot(Brushes.Goldenrod, NinjaTrader.Custom.Resource.NinjaScriptIndicatorNameSMA);
			}
			else if (State == State.Configure)
			{
				
			}
			else if (State == State.DataLoaded)
			{
				
			}
		}

		protected override void OnBarUpdate()
		{
//			if (BarsArray[0].BarsType.IsRemoveLastBarSupported)
//			{
//				if (CurrentBar == 0)
//					Value[0] = Input[0];
//				else
//				{

//				}
//			}
//			else
//			{
				if (IsFirstTickOfBar)
					priorSum = sum;
				
				if (CurrentBar == Period){
					sum = 0;
					for (int i = Period-1; i >= 0; i--){
						sum += Input[i];	
					}
					Value[0] = sum / Period;
				}
				else if (CurrentBar > Period){
					sum = priorSum - Value[1] + Input[0];
					Value[0] = sum / Period;
				}
//			}
		}

		#region Properties
		[Range(1, int.MaxValue), NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Period", GroupName = "NinjaScriptParameters", Order = 0)]
		public int Period
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SSA[] cacheSSA;
		public SSA SSA(int period)
		{
			return SSA(Input, period);
		}

		public SSA SSA(ISeries<double> input, int period)
		{
			if (cacheSSA != null)
				for (int idx = 0; idx < cacheSSA.Length; idx++)
					if (cacheSSA[idx] != null && cacheSSA[idx].Period == period && cacheSSA[idx].EqualsInput(input))
						return cacheSSA[idx];
			return CacheIndicator<SSA>(new SSA(){ Period = period }, input, ref cacheSSA);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SSA SSA(int period)
		{
			return indicator.SSA(Input, period);
		}

		public Indicators.SSA SSA(ISeries<double> input , int period)
		{
			return indicator.SSA(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SSA SSA(int period)
		{
			return indicator.SSA(Input, period);
		}

		public Indicators.SSA SSA(ISeries<double> input , int period)
		{
			return indicator.SSA(input, period);
		}
	}
}

#endregion
