using FluentValidation.Resources;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Core.Api
{
    public class FluentValidationLanguageManager : ILanguageManager
	{
		private static readonly Regex RegexPatternIgnore = new Regex("^([a-z\\-]+\\/)+[a-z\\-]+$", RegexOptions.Compiled);
		private static readonly Regex RegexPatternReplaceInvalidChars = new Regex(@"[^A-Za-z\\_\\-]", RegexOptions.Compiled);
		private static readonly Regex RegexPatternReplaceUnderscore = new Regex("([A-Za-z])([_])([A-Za-z])", RegexOptions.Compiled);
		private static readonly Regex RegexPatternReplaceUpperCases = new Regex("([A-Za-z])([A-Z])", RegexOptions.Compiled);

		public string GetString(string key, CultureInfo culture = null)
		{
			if (RegexPatternIgnore.IsMatch(key))
			{
				return key;
			}

			var keyTmp = RegexPatternReplaceInvalidChars.Replace(key, string.Empty);
			keyTmp = RegexPatternReplaceUnderscore.Replace(keyTmp, "$1/$3");
			keyTmp = RegexPatternReplaceUpperCases.Replace(keyTmp, "$1-$2");

			return $"validation/{keyTmp}".ToLower();
		}

		public bool Enabled { get; set; }
		public CultureInfo Culture { get; set; }
	}
}
