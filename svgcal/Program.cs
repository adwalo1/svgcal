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

		public static void handleMonth(int year, int month)
		{

			var surface = new SvgSurface (month.ToString("00")+".svg", width, height);
			Cairo.Context cr = new Context(surface);

			cr.SetSourceRGBA(0.0, 0.0, 0.0, 1.0);

			var d = new DateTime (year, month,1);


			var DayRectArea = new Rectangle (
			                  MarginLeft, MarginUp + TotalHeader,
			                  ContentWidth, ContentHeight);
			//cr.Rectangle (DayRectArea);

			var MonthHeaderRect = new Rectangle (
				MarginLeft, MarginUp,
				ContentWidth, 
				MonthHeader);
			//cr.Rectangle (MonthHeaderRect);

			var Month= MonthNames[d.Month];
			cr.SetFontSize (MonthHeader*0.8);
			var te= cr.TextExtents (Month);
			cr.MoveTo (MarginLeft- te.XBearing,
				MarginUp+MonthHeader-((MonthHeader-te.Height)/2)-(te.Height+te.YBearing));
			cr.ShowText (Month);

			te = cr.TextExtents (year.ToString());
			cr.MoveTo (width-MarginRight-te.Width-te.XBearing, 
				MarginUp+MonthHeader-((MonthHeader-te.Height)/2)-(te.Height+te.YBearing));
			cr.ShowText (year.ToString());					

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
			te = cr.TextExtents ("Donnerstag");
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
				handleMonth (2017, i);

			Console.WriteLine ("Hello World!");
		}
	}
}
