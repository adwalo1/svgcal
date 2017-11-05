using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace svgcal
{
	
	class SimpleIcal
	{
		public Dictionary<string, List<string>> holidays;

		public SimpleIcal(string f)
		{
			var s= File.ReadAllText (f);

			holidays = new Dictionary<string, List<string>> ();


			var elements = s.Split (new [] {"BEGIN:VEVENT"}, System.StringSplitOptions.None);

			foreach (string block in elements) 
			{
				Regex r1 = new Regex(@".*SUMMARY:([\w .-]*)");

				Match holidayName = r1.Match(block);
				if (holidayName.Success)
				{
					Regex r2 = new Regex(@".*DTSTART;VALUE=DATE:(.*)\n");

					Match date = r2.Match(block);
					if (date.Success) {

						string name = holidayName.Groups [1].ToString ().Trim();
						string d = date.Groups [1].ToString().Trim();

						if (!holidays.ContainsKey (d)) {
							holidays [d] = new List<string> ();
						}

						holidays [d].Add (name);


					}
				}
			}

		}
	}
}

