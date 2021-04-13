using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Translate.V3;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace xlator
{
    class Program
    {
        static string language = "es";
        static MongoClient xlatorClient;
        static IMongoCollection<TextBlock> colBlobs;
        static MongoClient snootyClient;
        static IMongoCollection<BsonDocument> colSnoots;

        private static List<ReplaceOneModel<BsonDocument>> docsToReplace;

        static BsonDocument translatedDoc;

        static async Task Main(string[] args)
        {
            docsToReplace = new List<ReplaceOneModel<BsonDocument>>();

            xlatorClient = new MongoClient("mongodb+srv://snoot:SnootSnoot@cluster0.xoid4.mongodb.net/myFirstDatabase?retryWrites=true&w=majority");
            colBlobs = xlatorClient.GetDatabase("xlator").GetCollection<TextBlock>("blocks");

            snootyClient = new MongoClient("mongodb+srv://caleb:6lQ0Qr8cnkFPRwQg@cluster0-ylwlz.mongodb.net/test?authSource=admin&replicaSet=Cluster0-shard-0&readPreference=primary&appname=MongoDB%20Compass&retryWrites=true&ssl=true");
            colSnoots = snootyClient.GetDatabase("snooty_dev").GetCollection<BsonDocument>("documents");

            var filter = new BsonDocument("page_id", BsonRegularExpression.Create("realm/caleb/skunkworks/deploy"));
            Console.WriteLine(colSnoots.CountDocuments(filter));

            int counter = 1;
            await colSnoots.Find(filter).ForEachAsync(mainDoc =>
            {
                translatedDoc = mainDoc;
                //Console.WriteLine(mainDoc);
                Console.WriteLine(counter);
                counter++;

                if (mainDoc.Contains("ast"))
                {
                    var ast = mainDoc.GetValue("ast").AsBsonDocument;
                    GetTextValues(ast, mainDoc);
                }


                //colSnoots.ReplaceOne(new BsonDocument("_id", mainDoc.GetValue("_id")), translatedDoc);

                if (docsToReplace.Count > 0)
                {
                    colSnoots.BulkWrite(docsToReplace);
                }

            });
        }

        private static void GetTextValues(BsonDocument d, BsonDocument mainDoc)
        {
            foo++;

            if (d.Contains("children"))
            {
                var kiddos = d.GetValue("children").AsBsonArray;
                foreach (BsonDocument child in kiddos)
                {
                    if (child.Contains("type") && child.GetValue("type") == "text")
                    {
                        ProcessTextNode(child.GetValue("value").ToString(), mainDoc);
                    }

                    if (child.Contains("children"))
                    {
                        GetTextValues(child, mainDoc);
                    }
                }
            }
        }


        private static void ProcessTextNode(string englishText, BsonDocument mainDoc)
        {

            if (englishText == String.Empty)
            {
                return;
            }

            var existing = colBlobs.Find<TextBlock>(tb => tb.SourceText == englishText).FirstOrDefault();
            string xlatedText = String.Empty;
            if (existing == null)
            {
                //This is either a new text blob, or an old one that has
                // changed source text (which is, in this case, "new")
                xlatedText = TranslateTo(language, englishText);

                var enText = new TextBlock()
                {
                    CreatedAt = DateTimeOffset.UtcNow,
                    SourceText = englishText,
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

                translatedDoc = BsonSerializer.Deserialize<BsonDocument>(
                    translatedDoc.ToString().Replace(englishText, xlatedText));

                colBlobs.InsertOne(enText);
            }
            else if (!existing.Translations.ContainsKey(language))
            { //we have a doc already, but no translation for this language

                xlatedText = TranslateTo(language, englishText);

                var xlateText = new TranslationBlock()
                {
                    Auto = xlatedText
                };

                translatedDoc = BsonSerializer.Deserialize<BsonDocument>(
                    translatedDoc.ToString().Replace(englishText, xlatedText));

                var update = Builders<TextBlock>.Update.Set("translations",
                    new Dictionary<string, TranslationBlock>()
                    {{ language, xlateText } });

                colBlobs.UpdateOne<TextBlock>(e => e.Id == existing.Id, update);
            }
            else
            {
                // the english hasn't changed, so if there's already translated text,
                // don't bother re-translating!
            }

            if (xlatedText != String.Empty)
            {
                //translatedDoc = BsonSerializer.Deserialize<BsonDocument>(docasstring);
                //BsonDocument document = BsonSerializer.Deserialize<BsonDocument>(docasstring);
                var filterForUpdate = Builders<BsonDocument>.Filter.Eq("_id", mainDoc.GetValue("_id"));
                docsToReplace.Add(new ReplaceOneModel<BsonDocument>(filterForUpdate, translatedDoc));
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