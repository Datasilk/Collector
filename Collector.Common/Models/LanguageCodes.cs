using System.Collections.Generic;

namespace Collector.Common.Models
{
    /// <summary>
    /// Provides a dictionary of ISO 639-1 language codes and their corresponding language names.
    /// Includes regional variants where applicable.
    /// </summary>
    public static class LanguageCodes
    {
        /// <summary>
        /// Dictionary of ISO 639-1 codes as keys and language names (with optional region) as values
        /// </summary>
        public static readonly Dictionary<string, string> Codes = new Dictionary<string, string>
        {
            // Major languages
            {"en", "English"},
            {"en-US", "English (United States)"},
            {"en-GB", "English (United Kingdom)"},
            {"en-CA", "English (Canada)"},
            {"en-AU", "English (Australia)"},
            {"es", "Spanish"},
            {"es-ES", "Spanish (Spain)"},
            {"es-MX", "Spanish (Mexico)"},
            {"es-AR", "Spanish (Argentina)"},
            {"fr", "French"},
            {"fr-FR", "French (France)"},
            {"fr-CA", "French (Canada)"},
            {"fr-BE", "French (Belgium)"},
            {"de", "German"},
            {"de-DE", "German (Germany)"},
            {"de-AT", "German (Austria)"},
            {"de-CH", "German (Switzerland)"},
            {"it", "Italian"},
            {"it-IT", "Italian (Italy)"},
            {"pt", "Portuguese"},
            {"pt-BR", "Portuguese (Brazil)"},
            {"pt-PT", "Portuguese (Portugal)"},
            {"ru", "Russian"},
            {"zh", "Chinese"},
            {"zh-CN", "Chinese (Simplified)"},
            {"zh-TW", "Chinese (Traditional)"},
            {"ja", "Japanese"},
            {"ko", "Korean"},
            {"ar", "Arabic"},
            
            // Other languages (alphabetical)
            {"aa", "Afar"},
            {"ab", "Abkhazian"},
            {"ae", "Avestan"},
            {"af", "Afrikaans"},
            {"ak", "Akan"},
            {"am", "Amharic"},
            {"an", "Aragonese"},
            {"as", "Assamese"},
            {"av", "Avaric"},
            {"ay", "Aymara"},
            {"az", "Azerbaijani"},
            {"ba", "Bashkir"},
            {"be", "Belarusian"},
            {"bg", "Bulgarian"},
            {"bh", "Bihari languages"},
            {"bi", "Bislama"},
            {"bm", "Bambara"},
            {"bn", "Bengali"},
            {"bo", "Tibetan"},
            {"br", "Breton"},
            {"bs", "Bosnian"},
            {"ca", "Catalan"},
            {"ce", "Chechen"},
            {"ch", "Chamorro"},
            {"co", "Corsican"},
            {"cr", "Cree"},
            {"cs", "Czech"},
            {"cu", "Church Slavic"},
            {"cv", "Chuvash"},
            {"cy", "Welsh"},
            {"da", "Danish"},
            {"dv", "Divehi"},
            {"dz", "Dzongkha"},
            {"ee", "Ewe"},
            {"el", "Greek"},
            {"eo", "Esperanto"},
            {"et", "Estonian"},
            {"eu", "Basque"},
            {"fa", "Persian"},
            {"ff", "Fulah"},
            {"fi", "Finnish"},
            {"fj", "Fijian"},
            {"fo", "Faroese"},
            {"fy", "Western Frisian"},
            {"ga", "Irish"},
            {"gd", "Gaelic"},
            {"gl", "Galician"},
            {"gn", "Guarani"},
            {"gu", "Gujarati"},
            {"gv", "Manx"},
            {"ha", "Hausa"},
            {"he", "Hebrew"},
            {"hi", "Hindi"},
            {"ho", "Hiri Motu"},
            {"hr", "Croatian"},
            {"ht", "Haitian"},
            {"hu", "Hungarian"},
            {"hy", "Armenian"},
            {"hz", "Herero"},
            {"ia", "Interlingua"},
            {"id", "Indonesian"},
            {"ie", "Interlingue"},
            {"ig", "Igbo"},
            {"ii", "Sichuan Yi"},
            {"ik", "Inupiaq"},
            {"io", "Ido"},
            {"is", "Icelandic"},
            {"iu", "Inuktitut"},
            {"jv", "Javanese"},
            {"ka", "Georgian"},
            {"kg", "Kongo"},
            {"ki", "Kikuyu"},
            {"kj", "Kuanyama"},
            {"kk", "Kazakh"},
            {"kl", "Kalaallisut"},
            {"km", "Central Khmer"},
            {"kn", "Kannada"},
            {"kr", "Kanuri"},
            {"ks", "Kashmiri"},
            {"ku", "Kurdish"},
            {"kv", "Komi"},
            {"kw", "Cornish"},
            {"ky", "Kirghiz"},
            {"la", "Latin"},
            {"lb", "Luxembourgish"},
            {"lg", "Ganda"},
            {"li", "Limburgan"},
            {"ln", "Lingala"},
            {"lo", "Lao"},
            {"lt", "Lithuanian"},
            {"lu", "Luba-Katanga"},
            {"lv", "Latvian"},
            {"mg", "Malagasy"},
            {"mh", "Marshallese"},
            {"mi", "Maori"},
            {"mk", "Macedonian"},
            {"ml", "Malayalam"},
            {"mn", "Mongolian"},
            {"mr", "Marathi"},
            {"ms", "Malay"},
            {"mt", "Maltese"},
            {"my", "Burmese"},
            {"na", "Nauru"},
            {"nb", "Norwegian Bokmål"},
            {"nd", "North Ndebele"},
            {"ne", "Nepali"},
            {"ng", "Ndonga"},
            {"nl", "Dutch"},
            {"nl-BE", "Dutch (Belgium)"},
            {"nl-NL", "Dutch (Netherlands)"},
            {"nn", "Norwegian Nynorsk"},
            {"no", "Norwegian"},
            {"nr", "South Ndebele"},
            {"nv", "Navajo"},
            {"ny", "Chichewa"},
            {"oc", "Occitan"},
            {"oj", "Ojibwa"},
            {"om", "Oromo"},
            {"or", "Oriya"},
            {"os", "Ossetian"},
            {"pa", "Punjabi"},
            {"pi", "Pali"},
            {"pl", "Polish"},
            {"ps", "Pushto"},
            {"qu", "Quechua"},
            {"rm", "Romansh"},
            {"rn", "Rundi"},
            {"ro", "Romanian"},
            {"ro-RO", "Romanian (Romania)"},
            {"ro-MD", "Romanian (Moldova)"},
            {"rw", "Kinyarwanda"},
            {"sa", "Sanskrit"},
            {"sc", "Sardinian"},
            {"sd", "Sindhi"},
            {"se", "Northern Sami"},
            {"sg", "Sango"},
            {"si", "Sinhala"},
            {"sk", "Slovak"},
            {"sl", "Slovenian"},
            {"sm", "Samoan"},
            {"sn", "Shona"},
            {"so", "Somali"},
            {"sq", "Albanian"},
            {"sr", "Serbian"},
            {"ss", "Swati"},
            {"st", "Southern Sotho"},
            {"su", "Sundanese"},
            {"sv", "Swedish"},
            {"sv-SE", "Swedish (Sweden)"},
            {"sv-FI", "Swedish (Finland)"},
            {"sw", "Swahili"},
            {"ta", "Tamil"},
            {"te", "Telugu"},
            {"tg", "Tajik"},
            {"th", "Thai"},
            {"ti", "Tigrinya"},
            {"tk", "Turkmen"},
            {"tl", "Tagalog"},
            {"tn", "Tswana"},
            {"to", "Tonga"},
            {"tr", "Turkish"},
            {"ts", "Tsonga"},
            {"tt", "Tatar"},
            {"tw", "Twi"},
            {"ty", "Tahitian"},
            {"ug", "Uighur"},
            {"uk", "Ukrainian"},
            {"ur", "Urdu"},
            {"uz", "Uzbek"},
            {"ve", "Venda"},
            {"vi", "Vietnamese"},
            {"vo", "Volapük"},
            {"wa", "Walloon"},
            {"wo", "Wolof"},
            {"xh", "Xhosa"},
            {"yi", "Yiddish"},
            {"yo", "Yoruba"},
            {"za", "Zhuang"},
            {"zu", "Zulu"}
        };

        /// <summary>
        /// Gets the language name for a given ISO 639-1 code
        /// </summary>
        /// <param name="code">The ISO 639-1 code</param>
        /// <returns>The language name or the code itself if not found</returns>
        public static string GetLanguageName(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            return Codes.TryGetValue(code, out string name) ? name : code;
        }

        /// <summary>
        /// Checks if the provided code is a valid ISO 639-1 language code
        /// </summary>
        /// <param name="code">The code to check</param>
        /// <returns>True if the code is valid, false otherwise</returns>
        public static bool IsValidCode(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return false;
            }

            return Codes.ContainsKey(code);
        }
    }
}
