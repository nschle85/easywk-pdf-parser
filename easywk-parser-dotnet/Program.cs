﻿// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.Util;

namespace easywk_parser_dotnet
{
    class EasywkParser
    {
        private readonly PdfDocument _document;
        
        private Regex _laufRegex = new Regex(@"(Lauf) (\d+)/(\d+) (.*)");
        
        private string _wettkampf = "";
        private string _lauf = "";

        
        public static void Main(string[] args)
        {
            // Display the number of command line arguments.
            Console.WriteLine("Arguments count: " + args.Length);
            Console.WriteLine("Hello, World!");
            var filePath = @"/Users/nschle85/IdeaProjects/MeldelisteParser/meldeliste.pdf";
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                var parser = new EasywkParser(document);
                var result = parser.Parse();
                using (StreamWriter outputFile = new StreamWriter(filePath + "-fromdotnet.csv"))
                {
                    outputFile.WriteLine("Wettkampf\tLauf\tBahn\tSchwimmer\tVerein\tMeldezeit");
                    foreach (var starterLine in result)
                    {
                        Console.WriteLine(starterLine.toString());
                        outputFile.WriteLine(starterLine.toString());
                    }
                }
            }
        }

        public EasywkParser(PdfDocument document)
        {
            this._document = document;
        }

        public List<StarterLine> Parse()
        {
            List<StarterLine> result = []; 
            foreach (var page in _document.GetPages())
            {
                var pageText = page.Text;
                var words = DefaultWordExtractor.Instance.GetWords(page.Letters);
                var blocks = DefaultPageSegmenter.Instance.GetBlocks(words);
                foreach (var textBlock in blocks)
                {
                    foreach (var textBlockTextLine in textBlock.TextLines)
                    {
                        var line = ParseTextLine(textBlockTextLine);
                        if (line!=null)
                        {
                            result.Add(line);
                        }
                    }
                }
            }

            return result;
        }

        private StarterLine? ParseTextLine(TextLine textLine)
        {
            StarterLine result = null;
            var line = textLine.Text;
            //Console.WriteLine(line);
            
            if (line.StartsWith("Wettkampf"))
            {
                _wettkampf = line;
            }
            else if (line.StartsWith("Lauf"))
            {
                // Matcher laufMatcher = laufPattern.matcher(line);
                var  laufMatcher = _laufRegex.Match(line);
                if (laufMatcher.Success)
                {
                    _lauf = laufMatcher.Groups[1].Value + " " + toLeadingZeroString(laufMatcher.Groups[2].Value) + "/" + toLeadingZeroString(laufMatcher.Groups[3].Value) + " " + laufMatcher.Groups[4].Value;
                }
                else
                {
                    Console.Error.WriteLine("No matching Lauf:" + line);
                }
            }
            else if (line.StartsWith("Bahn"))
            {
                // match (Bahn), (BahnNummer), (schwimmer: Teilnehmer, Jahrgang, Verein etc), (Meldezeit)
                var r = new Regex(@"(Bahn) (\d+) (.+?) (\d+:\d+,\d+)");
                // Now create matcher object.
                var m = r.Match(line);
                if (m.Success)
                {
                    // var bahn = m.group(1);
                    var bahnNr = m.Groups[2].Value;
                    var schwimmer = m.Groups[3].Value;

                    //extract verein if possible and update schwimmer and verein
                    string verein = "";

                    // Phdksfh, Fdsds  2013/ TSV Neuburg
                    var schwimmerVereinPattern = new Regex(@"(.+?)\/ (.*)");
                    var schwimmerVereinMatcher = schwimmerVereinPattern.Match(schwimmer);

                    // Rdsds, Sfdfd  1978/AK 45	SV Lohhof
                    var schwimmerAkVereinPattern = new Regex(@"(.+?\/AK \d+) (.*)");
                    var schwimmerAkVereinMatcher = schwimmerAkVereinPattern.Match(schwimmer);

                    //Rdsds, Sfdfd  1978 45	SV Lohhof
                    var schwimmerJgVereinPattern = new Regex(@"(.+? \d+) (.*)");
                    var schwimmerJgVereinMatcher = schwimmerJgVereinPattern.Match(schwimmer);

                    //Rdsds, Sfdfd  Offen 45 SV Lohhof
                    var schwimmerOffenVereinPattern = new Regex(@"(.+? Offen) (.*)");
                    var schwimmerOffenVereinMatcher = schwimmerOffenVereinPattern.Match(schwimmer);

                    if (schwimmerVereinMatcher.Success) {
                             schwimmer = schwimmerVereinMatcher.Groups[1].Value;
                             verein = schwimmerVereinMatcher.Groups[2].Value;
                    }
                    else if (schwimmerAkVereinMatcher.Success) {
                             schwimmer = schwimmerAkVereinMatcher.Groups[1].Value;
                             verein = schwimmerAkVereinMatcher.Groups[2].Value;
                    }
                    else if (schwimmerJgVereinMatcher.Success) {
                             schwimmer = schwimmerJgVereinMatcher.Groups[1].Value;
                             verein = schwimmerJgVereinMatcher.Groups[2].Value;
                    }
                    else if (schwimmerOffenVereinMatcher.Success) {
                             schwimmer = schwimmerOffenVereinMatcher.Groups[1].Value;
                             verein = schwimmerOffenVereinMatcher.Groups[2].Value;
                    }

                    else {
                            Console.Error.WriteLine("Could not parse: " + schwimmer);
                    }
                    
                    var meldezeit = m.Groups[4].Value;
                    
                    result = new StarterLine(_wettkampf, _lauf, bahnNr, schwimmer, verein, meldezeit);
                }
            }
            else
            {
                Console.Error.WriteLine("NO MATCH "+ line);
            }

            return result;
        }
        
        private string toLeadingZeroString(string value)
        {
            return Int32.Parse(value).ToString("00");
        }
    }
    
}


