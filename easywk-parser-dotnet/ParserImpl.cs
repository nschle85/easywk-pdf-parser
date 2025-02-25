using System.Text.RegularExpressions;

namespace easywk_parser_dotnet;

public class ParserImpl : IParser
{
    private readonly IList<string> _textlines;
    private string _wettkampf = "";
    private string _lauf = "";
    
    private readonly Regex _wettkampfRegex = new Regex(@"(Wettkampf) (\d+) (.*)");
    private readonly Regex _laufRegex = new Regex(@"(Lauf) (\d+)/(\d+) (.*)");
    private readonly Regex _laufShortRegex = new Regex(@"(Lauf) (\d+)/(\d+)");
    // match (Bahn), (BahnNummer), (schwimmer: Teilnehmer, Jahrgang, Verein etc), (Meldezeit)
    private readonly Regex _bahnRegex = new Regex(@"(Bahn) (\d+) (.+?) (\d+:\d+,\d+)");
    
    //Splash Meet Manager (SSM), 11.79888
    //private readonly Regex _wettkampfRegexNew = new Regex(@"(Wettkampf) (\d+),(.*)");
    private readonly Regex _laufRegexSsm = new Regex(@"(Lauf) (\d+) von (\d+)(.*)");
    private readonly Regex _bahnRegexSsm = new Regex(@"^(\d+) (.*?)");
    private readonly Regex _bahnRegexSsmLiRe = new Regex(@"^(li|re) (\d+) (.+?) ((\d+:\d+\.\d+)|(NT)|(\d+\.\d+))");
    
    private readonly Regex _schwimmerVereinCountryRegexSsm = new Regex(@"(.+?) (GER|ROU) (.*)");
    
    private readonly Regex _wasteRegex = new Regex(@"^((Ausrichter:)|(Splash Meet Manager,)|(\d+))|((Startliste)|(Mastersschwimmen))(.*?)");


    
    public ParserImpl(IList<string> textLines)
    {
        this._textlines = textLines;
    }
    
    public List<StarterLine> Parse()
    {
        List<StarterLine> result = []; 
        foreach (var textBlockTextLine in _textlines)
        { 
            var line = ParseTextLine(textBlockTextLine);
            if (line!=null)
            {
                result.Add(line);
            }
        }

        return result;
    }

    private StarterLine? ParseTextLine(string line)
    {
        StarterLine? result = null;
        //Console.WriteLine(line);
        
        if (line.StartsWith("Wettkampf"))
        {
            var wettkampfMatcher = _wettkampfRegex.Match(line);
            //var wettkampfMatcherNew = _wettkampfRegexNew.Match(line);
            if (wettkampfMatcher.Success)
            {
                _wettkampf = wettkampfMatcher.Groups[1].Value + " " + ToLeadingZeroString(wettkampfMatcher.Groups[2].Value) + " " + wettkampfMatcher.Groups[3].Value;
                Console.WriteLine("Found parsed :"+_wettkampf);
            }
            else
            {
                Console.Error.WriteLine("No matching Wettkampf:" + line);
            }
        }
        else if (line.StartsWith("Lauf"))
        {
            // Matcher laufMatcher = laufPattern.matcher(line);
            var laufMatcher = _laufRegex.Match(line);
            var laufShortMatcher = _laufShortRegex.Match(line);
            var laufMatcherNew = _laufRegexSsm.Match(line);
            if (laufMatcher.Success)
            {
                _lauf = laufMatcher.Groups[1].Value + " " + ToLeadingZeroString(laufMatcher.Groups[2].Value) + 
                        "/" + ToLeadingZeroString(laufMatcher.Groups[3].Value) + " " + laufMatcher.Groups[4].Value;
            }
            if (laufShortMatcher.Success)
            {
                _lauf = laufShortMatcher.Groups[1].Value + " " + ToLeadingZeroString(laufShortMatcher.Groups[2].Value) + 
                        "/" + ToLeadingZeroString(laufShortMatcher.Groups[3].Value);
            }
            else if (laufMatcherNew.Success)
            {
                _lauf = laufMatcherNew.Groups[1].Value + " " + ToLeadingZeroString(laufMatcherNew.Groups[2].Value) +
                        "/" + ToLeadingZeroString(laufMatcherNew.Groups[3].Value);
            }
            else
            {
                Console.Error.WriteLine("No matching Lauf:" + line);
            }
        }
        else if (line.StartsWith("Bahn"))
        {
            // match (Bahn), (BahnNummer), (schwimmer: Teilnehmer, Jahrgang, Verein etc), (Meldezeit)
            var r = _bahnRegex;
            // Now create matcher object.
            var m = r.Match(line);
            if (m.Success)
            {
                // var bahn = m.group(1);
                var bahnNr = m.Groups[2].Value;

                var schwimmerVereinString = m.Groups[3].Value;
                string verein = "";
                SchwimmerVerein schwimmerVerein = new SchwimmerVerein(schwimmerVereinString, verein);

                //extract verein if possible and update schwimmer and verein
                

                // Phdksfh, Fdsds  2013/ TSV Neuburg
                var schwimmerVereinPattern = new Regex(@"(.+?)\/ (.*)");
                var schwimmerVereinMatcher = schwimmerVereinPattern.Match(schwimmerVereinString);

                // Rdsds, Sfdfd  1978/AK 45	SV Lohhof
                var schwimmerAkVereinPattern = new Regex(@"(.+?\/AK \d+) (.*)");
                var schwimmerAkVereinMatcher = schwimmerAkVereinPattern.Match(schwimmerVereinString);
                
                // Rdsds, Sfdfd  1978/Jugend A SV Lohhof
                var schwimmerJugendVereinPattern = new Regex(@"(.+?\/(Jugend A|Jugend B|Jugend C|Jugend D|Junioren)) (.*)");
                var schwimmerJugendVereinMatcher = schwimmerJugendVereinPattern.Match(schwimmerVereinString);
                

                //Rdsds, Sfdfd  1978 45	SV Lohhof
                var schwimmerJgVereinPattern = new Regex(@"(.+? \d+) (.*)");
                var schwimmerJgVereinMatcher = schwimmerJgVereinPattern.Match(schwimmerVereinString);

                //Rdsds, Sfdfd  Offen 45 SV Lohhof
                var schwimmerOffenVereinPattern = new Regex(@"(.+? Offen) (.*)");
                var schwimmerOffenVereinMatcher = schwimmerOffenVereinPattern.Match(schwimmerVereinString);

                if (schwimmerVereinMatcher.Success) { 
                    schwimmerVerein= new SchwimmerVerein(schwimmerVereinMatcher.Groups[1].Value, schwimmerVereinMatcher.Groups[2].Value);
                }
                else if (schwimmerAkVereinMatcher.Success)
                {
                    schwimmerVerein = new SchwimmerVerein(schwimmerAkVereinMatcher.Groups[1].Value,schwimmerAkVereinMatcher.Groups[2].Value);
                }
                else if (schwimmerJugendVereinMatcher.Success)
                {
                    schwimmerVerein = new SchwimmerVerein(schwimmerJugendVereinMatcher.Groups[1].Value,schwimmerJugendVereinMatcher.Groups[3].Value);
                }
                else if (schwimmerJgVereinMatcher.Success) {
                    schwimmerVerein=new SchwimmerVerein(schwimmerJgVereinMatcher.Groups[1].Value,schwimmerJgVereinMatcher.Groups[2].Value);
                }
                else if (schwimmerOffenVereinMatcher.Success)
                {
                    schwimmerVerein = new SchwimmerVerein(schwimmerOffenVereinMatcher.Groups[1].Value, schwimmerOffenVereinMatcher.Groups[2].Value);
                }

                else {
                        Console.Error.WriteLine("Could not parse: " + schwimmerVereinString);
                }
                
                var meldezeit = m.Groups[4].Value;
                
                result = new StarterLine(_wettkampf, _lauf, "Bahn "+bahnNr, schwimmerVerein.Schwimmer, schwimmerVerein.Verein, meldezeit);
            }
        }
        else if (_bahnRegexSsm.Match(line).Success)
        {
            // match (BahnNummer), (schwimmer: Teilnehmer, Jahrgang, Verein etc), (Meldezeit)
            //var r = new Regex(@"^(\d+) (.*)");
            var r = new Regex(@"^(\d+) (.+?) ((\d+:\d+\.\d+)|(NT)|(\d+\.\d+))");
            // Now create matcher object.
            var m = r.Match(line);
            if (m.Success)
            {
                // var bahn = m.group(1);
                var bahnNr = m.Groups[1].Value;
                
                string schwimmerVereinString = m.Groups[2].Value;
                
                string verein = "";

                var schwimmerVerein = new SchwimmerVerein(schwimmerVereinString, verein);
                
                // NAME, Sirname - Erwin 1980 AK 40 ROU CSM Arad
                var schwimmerVereinCountryMatcher = _schwimmerVereinCountryRegexSsm.Match(schwimmerVereinString);
                
                // Rdsds, Sfdfd  1978/AK 45	SV Lohhof                                     
                var schwimmerAkVereinPattern = new Regex(@"(.+?\/AK \d+) (.*)");          
                var schwimmerAkVereinMatcher = schwimmerAkVereinPattern.Match(schwimmerVereinString); 
                
                if (schwimmerVereinCountryMatcher.Success) {
                    schwimmerVerein = new SchwimmerVerein(schwimmerVereinCountryMatcher.Groups[1].Value,schwimmerVereinCountryMatcher.Groups[3].Value);
                }
                else if (schwimmerAkVereinMatcher.Success) {
                    schwimmerVerein = new SchwimmerVerein(schwimmerAkVereinMatcher.Groups[1].Value, schwimmerAkVereinMatcher.Groups[2].Value);
                }
                else
                {
                    Console.WriteLine("Cannot parse Schwimmer: "+schwimmerVereinString);
                }
                var meldezeit = m.Groups[3].Value;
                
                if (_wettkampf.Length>0 && _lauf.Length >0)
                {
                    result = new StarterLine(_wettkampf, _lauf, bahnNr, schwimmerVerein.Schwimmer, schwimmerVerein.Verein, meldezeit);
                    //Console.WriteLine("Matched new bahn" + line);
                }
                
            }
            else
            {
                Console.WriteLine("No Matched bahn "+ line);
            }
        }
        else if (_bahnRegexSsmLiRe.Match(line).Success)
        {
            var bahnLiReMatch = _bahnRegexSsmLiRe.Match(line);
            // "li 8" or "re 9" 
            var bahnString = bahnLiReMatch.Groups[1].Value + " " + bahnLiReMatch.Groups[2].Value;
            var bahnSchwimmerString = bahnLiReMatch.Groups[3].Value;
            var meldezeit = bahnLiReMatch.Groups[4].Value;
            
            Console.WriteLine("Found li re: "+bahnString+" "+bahnSchwimmerString+" "+meldezeit);
            result = new StarterLine(_wettkampf, _lauf, "Bahn "+bahnString, bahnSchwimmerString, "VEREIN", meldezeit);
            
        }
        else if (_wasteRegex.Match(line).Success)
        {
            
        }
        else
        {
            Console.Error.WriteLine("NO MATCH "+ line);
        }

        return result;
    }
    
    private string ToLeadingZeroString(string value)
    {
        return Int32.Parse(value).ToString("00");
    }
}