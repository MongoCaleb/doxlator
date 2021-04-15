using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Google.Cloud.Translate.V3;
using MongoDB.Driver;

namespace xlator
{
    class Program
    {
        static string _language = "";
        static string _source_branch = "";
        static string _new_branch = "";
        static MongoClient xlatorClient;
        static IMongoCollection<TextBlock> colBlobs;
        static MongoClient snootyClient;
        static IMongoCollection<SourceDocument> colSourceSnoots;
        static IMongoCollection<SourceDocument> colTranslatedSnoots;

        private static List<WriteModel<TextBlock>> textBlocksToInsert;
        private static List<ReplaceOneModel<SourceDocument>> translatedDocsToUpload;

        private static int _not_cached;
        private static int _hit_cache;
        private static bool _forceCacheFlush = false;
        private static bool _debugMode = false;

        static void Main(string[] args)
        {
            HandleArgs(args);


            translatedDocsToUpload = new List<ReplaceOneModel<SourceDocument>>();
            textBlocksToInsert = new List<WriteModel<TextBlock>>();

            xlatorClient = new MongoClient("mongodb+srv://snoot:SnootSnoot@cluster0.xoid4.mongodb.net/myFirstDatabase?retryWrites=true&w=majority");
            colBlobs = xlatorClient.GetDatabase("xlator").GetCollection<TextBlock>("blocks");

            snootyClient = new MongoClient("mongodb+srv://caleb:6lQ0Qr8cnkFPRwQg@cluster0-ylwlz.mongodb.net/test?authSource=admin&replicaSet=Cluster0-shard-0&readPreference=primary&appname=MongoDB%20Compass&retryWrites=true&ssl=true");
            colSourceSnoots = snootyClient.GetDatabase("snooty_dev").GetCollection<SourceDocument>("documents");
            colTranslatedSnoots = snootyClient.GetDatabase("snooty_dev").GetCollection<SourceDocument>($"documents");

            var filter = Builders<SourceDocument>.Filter.Regex("page_id", $"realm/caleb/{_source_branch}");
            Console.WriteLine($"This corpus contains {colSourceSnoots.CountDocuments(filter)} " +
                $"documents to process, each of which may have many text blobs to translate to '{_language}'." +
                $"\r\nSo sit back, grab a cuppa, and enjoy the show.");
            if (_debugMode) Console.WriteLine(DateTime.Now.ToShortTimeString());

            int counter = 0;
            var colTemp = colSourceSnoots.Find(filter).ToList();
            Console.Write($"\r0%   ");

            Parallel.ForEach(colTemp, mainDoc =>
            {
                if (mainDoc.ast.CatchAll != null)
                {
                    throw new Exception();
                }

                if (mainDoc.ast != null)
                {
                    GetTextValues(mainDoc.ast);
                }

                mainDoc.page_id = mainDoc.page_id.Replace(_source_branch, _new_branch);

                var filterForUpdate = Builders<SourceDocument>.Filter.Where(d => d._id == mainDoc._id);
                translatedDocsToUpload.Add(new ReplaceOneModel<SourceDocument>(filterForUpdate, mainDoc) { IsUpsert = true });

                Console.Write($"\r{(double)counter / colTemp.Count * 100:#.0}%   ");
                counter++;
            });

            if (translatedDocsToUpload.Count > 0)
            {
                Console.WriteLine($"Updating {translatedDocsToUpload.Count} pages to '{colTranslatedSnoots.CollectionNamespace}'.");
                var result = colTranslatedSnoots.BulkWrite(translatedDocsToUpload);
                Console.WriteLine($"Processed {result.ProcessedRequests.Count}.");
            }

            if (textBlocksToInsert.Count > 0)
            {
                Console.WriteLine($"Saving {textBlocksToInsert.Count} mapping documents.");
                var bwr = colBlobs.BulkWrite(textBlocksToInsert, new BulkWriteOptions() { IsOrdered = false });

            }
            Console.WriteLine("Done!");
            Console.WriteLine($"A total of {_not_cached} text blobs were sent to the translator." +
                $" {_hit_cache} blobs had been previously translated and those translations were re-used.");
            if (_debugMode) Console.WriteLine(DateTime.Now.ToShortTimeString());
        }

        private static void HandleArgs(string[] args)
        {
            foreach (string argument in args)
            {
                if (argument.ToLower().StartsWith("language"))
                {
                    string[] parts = argument.Split('=');
                    _language = parts[1];
                }
                else if (argument.ToLower().StartsWith("source_branch"))
                {
                    string[] parts = argument.Split('=');
                    _source_branch = parts[1];
                }
                else if (argument.ToLower().StartsWith("new_branch"))
                {
                    string[] parts = argument.Split('=');
                    _new_branch = parts[1];
                }
                else if (argument.ToLower().StartsWith("clean"))
                {
                    _forceCacheFlush = true;
                }
                else if (argument.ToLower().StartsWith("debug"))
                {
                    _debugMode = true;
                }
            }

            if (_language == String.Empty || _source_branch == String.Empty)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("You must specify both the target language and branch. Optionally provide a new branch name:");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("xlate langauge=es source_branch=myGitBranch new_branch=myGitBranch_es");
                Console.Read();
                Environment.Exit(-1);
            }
            if (_new_branch == String.Empty) _new_branch = _source_branch;

        }

        private static bool GetTextValues(Ast astBlock)
        {
            bool updated = false;
            if (astBlock.children.Count > 0)
            {
                foreach (var child in astBlock.children)
                {
                    if (Helpers.DoNotTranslateTypes.Contains(child.type))
                    {
                        continue;
                    }
                    if (child.type != null && child.type == "text" && child.value != null)
                    {
                        ProcessTextNode(child);
                    }
                    else if (Helpers.SpecialTranslateTypes.Contains(child.type))
                    {
                        foreach (var arg in child.argument)
                        {
                            if (arg.type == "text")
                            {
                                ProcessTextNode(arg);
                            }
                        }
                    }
                    //iterate,yo
                    if (child.children.Count > 0)
                    {
                        GetTextValues(child);
                    }
                }
            }

            return updated;
        }


        private static void ProcessTextNode(Ast node)
        {
            if (node.value == String.Empty)
            {
                return;
            }

            var existing = colBlobs.Find<TextBlock>(tb => tb.SourceText == node.value).FirstOrDefault();
            string xlatedText = String.Empty;

            if (existing != null && _forceCacheFlush == true)
            {
                try { existing.Translations.Remove(_language); }
                catch (Exception e) { }
            }

            if (existing == null)
            {
                //This is either a new text blob, or an old one that has
                // changed source text (which is, in this case, "new")
                _not_cached++;
                xlatedText = TranslateTo(_language, node.value);

                var enText = new TextBlock()
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    SourceText = node.value,
                    Translations = new Dictionary<string, TranslationBlock>()
                        {
                            {
                                _language, new TranslationBlock()
                                {
                                    Auto = xlatedText
                                }
                            }
                        }
                };

                node.value = xlatedText;
                textBlocksToInsert.Add(new InsertOneModel<TextBlock>(enText));

                return;
            }
            else if (!existing.Translations.ContainsKey(_language))
            {
                _not_cached++;
                //we have a doc already, but no translation for this language
                xlatedText = TranslateTo(_language, node.value);

                var xlateText = new TranslationBlock()
                {
                    Auto = xlatedText
                };

                node.value = xlatedText;

                var filterDefinition = Builders<TextBlock>.Filter.Eq(e => e.Id, existing.Id);

                var update = Builders<TextBlock>.Update.Set($"translations.{_language}",
                     xlateText);

                textBlocksToInsert.Add(new UpdateOneModel<TextBlock>(filterDefinition, update));

                return;
            }

            else
            {
                // we have a previously-translated text,
                // so return it without calling the translator
                _hit_cache++;
                node.value = existing.Translations[_language].Manual != null ?
                    existing.Translations[_language].Manual :
                    existing.Translations[_language].Auto;

                return;
            }
        }


        private static string TranslateTo(string language, string source)
        {
            if (source == String.Empty || source == " ") return source;

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                Environment.CurrentDirectory + "/../../../fresh-rampart-310520-4eafed6542f9.json");
            TranslationServiceClient client = TranslationServiceClient.Create();
            TranslateTextRequest request = new TranslateTextRequest
            {
                SourceLanguageCode = "en",
                Contents = { IgnoreList.ReplaceIgnoreWords(source) },
                TargetLanguageCode = language,
                Parent = "projects/fresh-rampart-310520",
                MimeType = "text/plain"
            };
            string xlation = client.TranslateText(request).Translations[0].TranslatedText;
            // total hack. Google translate is removing trailing spaces
            // c# API is very poorly documented, so this hack will have to do
            // for now. Same with the HtmlDecode happening in the next line.
            // Apparently you can tell the translator to use text instead of
            // html, but again, not obvious where/how it the C# APIs.
            if (source.EndsWith(" ")) xlation += " ";
            return HttpUtility.HtmlDecode(IgnoreList.ReAaddIgnoreWords(xlation));
        }
    }
}


// TODO: TOC
//TODO: batch xlate by document

//TODO: [[IN TEST]] titles of notes, etc. are not translated.
// type = directive && name="note" or warning or tip, for each argument if type is "text", transate value
/*{
            "type": "directive",
            "position": {},
            "children": [
             
            ],
            "domain": "",
            "name": "note",
            "argument": [
              {
                "type": "text",
                "position": {
                  "start": {
                    "line": 24
                  }
                },
                "value": "Versions Update on Realm Open"
              }
            ]
          },*/

//TODO : figure out why asyncfind cursor ins't parallelizing each mainDoc

// TODO: remove catchall

// TODO: use reverse API to check accuracy (or use MSFT?)


//TODO: command arg to specify subsection only?
// TODO: STRETCH: UI for manual xlation
// TODO: STRETCH: Github and Slack integrations




/*
        private static void Test(BsonDocument doc, string text)
        {
            var docasstring = doc.ToString().Replace(text, "this is translated text, ya know");

            BsonDocument document = BsonSerializer.Deserialize<BsonDocument>(docasstring);

            colSnoots.ReplaceOne(new BsonDocument("_id", doc.GetValue("_id")), document);
        }
           private static void QuickTest()
           {
               Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Environment.CurrentDirectory + "/../../../fresh-rampart-310520-4eafed6542f9.json");
               TranslationServiceClient client = TranslationServiceClient.Create();
               TranslateTextRequest request = new TranslateTextRequest
               {
                   SourceLanguageCode = "en",
                   Contents = { "Hello, my name is Jose and I like to go to the library" },
                   TargetLanguageCode = "es",
                   Parent = "projects/fresh-rampart-310520"
               };
               TranslateTextResponse response = client.TranslateText(request);
               // response.Translations will have one entry, because request.Contents has one entry.
               Translation translation = response.Translations[0];
               // Console.WriteLine($"Detected language: {translation.DetectedLanguageCode}");
               Console.WriteLine($"Translated text: {translation.TranslatedText}");

           }*/