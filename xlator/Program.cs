using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Translate.V3;
using MongoDB.Driver;

namespace xlator
{
    class Program
    {
        static string language = "es";
        static MongoClient xlatorClient;
        static IMongoCollection<TextBlock> colBlobs;
        static MongoClient snootyClient;
        static IMongoCollection<SourceDocument> colSnoots;

        private static List<ReplaceOneModel<SourceDocument>> docsToReplace;

        static async Task Main(string[] args)
        {
            docsToReplace = new List<ReplaceOneModel<SourceDocument>>();

            xlatorClient = new MongoClient("mongodb+srv://snoot:SnootSnoot@cluster0.xoid4.mongodb.net/myFirstDatabase?retryWrites=true&w=majority");
            colBlobs = xlatorClient.GetDatabase("xlator").GetCollection<TextBlock>("blocks");

            snootyClient = new MongoClient("mongodb+srv://caleb:6lQ0Qr8cnkFPRwQg@cluster0-ylwlz.mongodb.net/test?authSource=admin&replicaSet=Cluster0-shard-0&readPreference=primary&appname=MongoDB%20Compass&retryWrites=true&ssl=true");
            colSnoots = snootyClient.GetDatabase("snooty_dev").GetCollection<SourceDocument>("documents");

            var filter = Builders<SourceDocument>.Filter.Regex("page_id", "realm/caleb/skunkworks/deploy");
            Console.WriteLine(colSnoots.CountDocuments(filter));

            int counter = 1;
            await colSnoots
                .Find(filter)
                .ForEachAsync(mainDoc =>
            {
                Console.WriteLine(counter);
                counter++;

                bool updated = false;
                if (mainDoc.ast != null)
                {
                    updated = GetTextValues(mainDoc.ast);
                }
                if (updated)
                {
                    var filterForUpdate = Builders<SourceDocument>.Filter.Where(d => d._id == mainDoc._id);
                    docsToReplace.Add(new ReplaceOneModel<SourceDocument>(filterForUpdate, mainDoc));
                }
            });

            if (docsToReplace.Count > 0)
            {
                colSnoots.BulkWrite(docsToReplace);
            }
        }

        private static bool GetTextValues(Ast astBlock)
        {
            bool updated = false;
            if (astBlock.children.Count > 0)
            {
                foreach (var child in astBlock.children)
                {
                    if (child.type != null && child.type == "text" && child.value != null)
                    {
                        updated = ProcessTextNode(child);
                    }

                    if (child.children.Count > 0)
                    {
                        updated = GetTextValues(child);
                    }
                }
            }

            return updated;
        }


        private static bool ProcessTextNode(Ast node)
        {

            if (node.value == String.Empty)
            {
                return false;
            }

            var existing = colBlobs.Find<TextBlock>(tb => tb.SourceText == node.value).FirstOrDefault();
            string xlatedText = String.Empty;
            if (existing == null)
            {
                //This is either a new text blob, or an old one that has
                // changed source text (which is, in this case, "new")
                xlatedText = TranslateTo(language, node.value);

                var enText = new TextBlock()
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    SourceText = node.value,
                    Translations = new Dictionary<string, TranslationBlock>()
                        {
                            {
                                language, new TranslationBlock()
                                {
                                    Auto = xlatedText
                                }
                            }
                        }
                };

                node.value = xlatedText;

                colBlobs.InsertOne(enText);
                return true;
            }
            else if (!existing.Translations.ContainsKey(language))
            { //we have a doc already, but no translation for this language

                xlatedText = TranslateTo(language, node.value);

                var xlateText = new TranslationBlock()
                {
                    Auto = xlatedText
                };

                node.value = xlatedText;

                var update = Builders<TextBlock>.Update.Set("translations",
                    new Dictionary<string, TranslationBlock>()
                    {{ language, xlateText } });

                colBlobs.UpdateOne<TextBlock>(e => e.Id == existing.Id, update);
                return true;
            }
            else
            {
                // the english hasn't changed, so if there's already translated text,
                // don't bother re-translating!
                return false;
            }
        }


        private static string TranslateTo(string language, string source)
        {
            if (source == String.Empty) return source;

            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Environment.CurrentDirectory + "/../../../fresh-rampart-310520-4eafed6542f9.json");
            TranslationServiceClient client = TranslationServiceClient.Create();
            TranslateTextRequest request = new TranslateTextRequest
            {
                SourceLanguageCode = "en",
                Contents = { IgnoreList.ReplaceIgnoreWords(source) },
                TargetLanguageCode = language,
                Parent = "projects/fresh-rampart-310520"
            };
            TranslateTextResponse response = client.TranslateText(request);
            return IgnoreList.ReAaddIgnoreWords(response.Translations[0].TranslatedText);
        }
    }

}


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