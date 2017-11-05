using System;
using Cairo;
using System.Collections.Generic;


namespace svgcal
{
	class WordWrap
	{
		Cairo.Context cr;
		double maxTextLenght;

		public List<string> result;

		string currentString;

		public WordWrap (Cairo.Context c, double max)
		{
			cr = c;
			maxTextLenght = max;
			result = new List<string> ();
			currentString = "";
		}

		public void AddWord(string word)
		{
			var te = cr.TextExtents (currentString+" "+ word);
			if (te.Width > maxTextLenght) {
				if (string.IsNullOrEmpty (currentString)) {
					//The string is too long, but we only have a single word -> we'll have to 
					//keep this word anyway
					result.Insert(0,word);
					return;
				}
				//the new word won't fit into the line
				//-> finish the current line
				FinishLine ();
				//and handle the next word on its own
				AddWord (word);
			} else {
				currentString = currentString + " " + word;
			}
		}

		public void FinishLine()
		{
			//Only finish the line if we actually have something in the buffer
			if (!string.IsNullOrEmpty (currentString)) {
				result.Insert (0,currentString);
				currentString = "";
			}
		}
	}

	class MainClass
	{
		//somehow cairo measures everything by default in points...
		//so i'll use this factor to convert milimeters in points
		static double milimeter = 1/0.3528;

		static double width = 305* milimeter;
		static double height = 210* milimeter;
		static double MarginUp = 10* milimeter;
		static double MarginDn = 23 * milimeter;
		static double MarginLeft = 5 * milimeter;
		static double MarginRight= 55 * milimeter;

		static double MonthHeader = 0.1 * height;
		static double WeekdayHeader = 0.04 * height;
		static double TotalHeader = MonthHeader + WeekdayHeader;

		static double ContentWidth = width - MarginLeft - MarginRight;
		static double ContentHeight = height - MarginUp - MarginDn-TotalHeader;


		static string[] MonthNames =  {"Dummy","Januar", "Februar", "März", "April", 
			"Mai", "Juni", "Juli", "August", "September", "Oktober", "November", "Dezember"
		};

		static string[] DayNames = {"Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", 
			"Samstag", "Sonntag"
		};

		static string[] DayNamesShort = {"SO", "MO", "DI", "MI", "DO", "FR","SA"};



		static SimpleIcal ical;


		public static double TextHeight(Cairo.Context cr)
		{
			var te= cr.TextExtents ("J");
			return te.Height;
		}
		public static double TextUpMargin(Cairo.Context cr)
		{
			var te= cr.TextExtents ("J");
			return -te.YBearing;
		}

		public static double TextDownMargin(Cairo.Context cr)
		{
			var te= cr.TextExtents ("J");
			return te.Height+te.YBearing;
		}

		public static void PrintMonthHeader (Cairo.Context cr, int year, int month, bool outputMonthHeaderRect=false)
		{
			if (outputMonthHeaderRect) {
				var MonthHeaderRect = new Rectangle (
					                     MarginLeft, MarginUp,
					                     ContentWidth, 
					                     MonthHeader);
				cr.Rectangle (MonthHeaderRect);
			}

			var Month= MonthNames[month];
			cr.SetFontSize (MonthHeader*0.8);
			var te= cr.TextExtents (Month);
			cr.MoveTo (MarginLeft- te.XBearing,
				MarginUp+MonthHeader-((MonthHeader-te.Height)/2)-(te.Height+te.YBearing));
			cr.ShowText (Month);

			te = cr.TextExtents (year.ToString());
			cr.MoveTo (width-MarginRight-te.Width-te.XBearing, 
				MarginUp+MonthHeader-((MonthHeader-te.Height)/2)-(te.Height+te.YBearing));
			cr.ShowText (year.ToString());					
			
			
		}

		public static void outputDayBox(Cairo.Context cr, DateTime d, Rectangle r)
		{
			cr.SetSourceRGBA(0,0,0, 1.0);
			//cr.MoveTo (r.X, r.Y);
			cr.MoveTo (r.X, r.Y + r.Height);
			cr.LineTo (r.X+(r.Width), r.Y+r.Height);

			//cr.Rectangle (r);
			cr.Stroke();

			//calculate the width of the Day-Text
			cr.SetFontSize (r.Height*0.9);
			string daynumber = string.Format ("{0}", d.Day);
			var te= cr.TextExtents (daynumber);
			var Textwidth = te.Width;
			cr.SetFontSize (r.Height * 0.5);
			Textwidth += cr.TextExtents (DayNamesShort [(int)d.DayOfWeek]).Width;

			//Reset the Font-size
			cr.SetFontSize (r.Height*0.9);

			if (d.DayOfWeek == DayOfWeek.Sunday || d.DayOfWeek == DayOfWeek.Saturday) {

				//For weekends, make the text white on a black surface
				//start with the black box
				cr.SetSourceRGBA (0, 0, 0, 1.0);
				var blackbackground = new Rectangle (r.X, r.Y, Textwidth+3*milimeter, r.Height);

				cr.Rectangle (blackbackground);
				cr.Fill ();
				cr.Stroke ();

				//Now the white text
				cr.SetSourceRGBA (1, 1, 1, 1.0);
				cr.MoveTo (r.X+milimeter, r.Y+r.Height-milimeter);
				cr.ShowText (daynumber);
				cr.SetFontSize (r.Height * 0.5);
				cr.ShowText (DayNamesShort [(int)d.DayOfWeek]);
				cr.Stroke ();

			} else {
				cr.SetSourceRGBA (0, 0, 0, 1.0);
				cr.MoveTo (r.X+milimeter, r.Y+r.Height-milimeter);
				cr.ShowText (daynumber);
				cr.SetFontSize (r.Height*0.5);
				cr.ShowText (DayNamesShort[(int)d.DayOfWeek]);
				cr.Stroke();
			}

			string datestring = d.Year.ToString ("0000") + d.Month.ToString ("00") + d.Day.ToString("00");
			if (ical.holidays.ContainsKey (datestring)) {

				cr.SetSourceRGBA (0, 0, 0, 1.0);
				cr.SetFontSize (r.Height*0.3);
				cr.MoveTo (r.X+4*milimeter+Textwidth, r.Y+r.Height-milimeter);
				string padding = " ";
				foreach (var s in ical.holidays[datestring]) {
					cr.ShowText (padding+s);
					padding = ", ";
				}
				cr.Stroke ();
			}
		}

		public static void handleMonth2Column(int year, int month)
		{

			var surface = new SvgSurface (month.ToString("00")+".svg", width, height);
			Cairo.Context cr = new Context(surface);

			cr.SetSourceRGBA(0.0, 0.0, 0.0, 1.0);

			PrintMonthHeader (cr, year, month);
			var d = new DateTime (year, month,1);

			var lastDayOfMonth = DateTime.DaysInMonth(d.Year, d.Month);


			//start with the first column
			int currentDay;
			var BoxHeight = (ContentHeight+WeekdayHeader) / 15;
			var BoxWidth = ContentWidth - width / 2;

			for (; d.Day <= 15; d=d.AddDays(1)) {
				var DayBox = new Rectangle (
					MarginLeft, MarginUp + MonthHeader+(d.Day-1)*BoxHeight,
					BoxWidth, BoxHeight);
				
				outputDayBox (cr, d, DayBox);
			}

			for (; d.Month== month; d=d.AddDays(1)) {
				var DayBox = new Rectangle (
					MarginLeft+width/2, MarginUp + MonthHeader+(d.Day-16)*BoxHeight,
					BoxWidth, BoxHeight);

				outputDayBox (cr, d, DayBox);
			}

			cr.Stroke();
			surface.Finish ();
			
			
		}
		public static void handleMonth(int year, int month)
		{

			var surface = new SvgSurface (month.ToString("00")+".svg", width, height);
			Cairo.Context cr = new Context(surface);

			cr.SetSourceRGBA(0.0, 0.0, 0.0, 1.0);

			PrintMonthHeader (cr, year, month);

			var d = new DateTime (year, month,1);


			var DayRectArea = new Rectangle (
			                  MarginLeft, MarginUp + TotalHeader,
			                  ContentWidth, ContentHeight);
			//cr.Rectangle (DayRectArea);



			var WeekdayRect = new Rectangle (
				MarginLeft, MarginUp+MonthHeader,
				ContentWidth, 
				WeekdayHeader);
			cr.Rectangle (WeekdayRect);


			//In worst case we have 6 rows
			double RowHeight= DayRectArea.Height/6;
			//we always have 7 columns
			double Columnwidth= DayRectArea.Width/7;


			int row;
			int column;

			//Calculate the Fontsize so that it will fit nicely into the columnwidth
			cr.SetFontSize(WeekdayHeader);
			var te = cr.TextExtents ("Donnerstag");
			cr.SetFontSize (WeekdayHeader*(Columnwidth*0.9)/te.Width);

			for(column=0;column<7;column++)
			{
				cr.IdentityMatrix ();
				cr.Translate (((column + 0.5) * Columnwidth) + MarginLeft, 
					MarginUp + MonthHeader + TextUpMargin(cr) + (WeekdayHeader - TextHeight(cr)) / 2);
				te= cr.TextExtents (DayNames[column]);

				cr.MoveTo (-(te.Width/2)-te.XBearing,0);
				cr.ShowText (DayNames[column]);
			}

			cr.SetFontSize (WeekdayHeader);

			var lastDayOfMonth = DateTime.DaysInMonth(d.Year, d.Month);

			int currentday = 1;
			for(row=0;row<6;row++)
			{
				cr.IdentityMatrix ();
				var r = new Rectangle (MarginLeft,TotalHeader+MarginUp+row*RowHeight,
					ContentWidth,
					RowHeight);
				cr.Rectangle (r);

				for(column=0;column < 7;column++)
				{
					int day = (((int)d.DayOfWeek)+6)%7 ;
					if (!(row == 0 && column < day) ){

						cr.IdentityMatrix ();
						cr.Translate ((column * Columnwidth)+MarginLeft, 
							(row * RowHeight)+TotalHeader+MarginUp);
						r = new Rectangle (0,0,
							Columnwidth,
							RowHeight);

						cr.Rectangle (r);

						cr.SetFontSize (WeekdayHeader*0.8);
						string daynumber = string.Format ("{0}", currentday);
						te= cr.TextExtents (daynumber);
						cr.MoveTo (milimeter-te.XBearing, milimeter+TextUpMargin(cr));
						cr.ShowText (daynumber);

						cr.SetFontSize (WeekdayHeader*0.8/2);
						string datestring = d.Year.ToString ("0000") + d.Month.ToString ("00") + currentday.ToString("00");
						if (ical.holidays.ContainsKey (datestring)) {
							cr.IdentityMatrix ();
							cr.Translate ((column * Columnwidth)+MarginLeft, 
								((row+1 )* RowHeight)+TotalHeader+MarginUp);

							var ww = new WordWrap (cr, Columnwidth-2* milimeter);

							foreach (var s in ical.holidays[datestring])
							{
								var x= s.Split(' ');
								foreach (string w in x)
								{
									ww.AddWord(w);
								}
								ww.FinishLine ();
							}

														
							int i;
							for (i = 0; i < ww.result.Count; i++) {
								var s = ww.result[i];

								te= cr.TextExtents (s);
								cr.MoveTo (milimeter-te.XBearing, (-i* (TextHeight(cr)+milimeter))-TextDownMargin(cr)-milimeter);
								cr.ShowText (s);

							}

						}

						currentday++;

					}
					if (currentday > lastDayOfMonth) break;
				}
				if (currentday > lastDayOfMonth) break;

			}

			cr.Stroke();
			surface.Finish ();

		}

		public static void Main (string[] args)
		{

			ical = new SimpleIcal ("basic.ics");

			int i;
			for(i=1; i<=12;i++)
				handleMonth2Column (2018, i);

			Console.WriteLine ("Hello World!");
		}
	}
}
