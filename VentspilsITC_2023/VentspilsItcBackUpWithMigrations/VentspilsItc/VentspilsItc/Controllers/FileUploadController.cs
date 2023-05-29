using Microsoft.AspNetCore.Mvc;
using System;

using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Rank.Data;
using Microsoft.Identity.Client;
using System.Security.Claims;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using VentspilsItc.Models;
using Microsoft.AspNetCore.Identity;

namespace VentspilsItc.Controllers
{

    public struct Person
    {
        public string Name { get; set; }
        public int Score { get; set; }
        public int Id { get; set; }
        public int ScoreId { get; set; }
        public Person(string name, int score, int id, int scoreId)
        {
            Id = id;
            Score = score;
            ScoreId = scoreId;
            Name = name;
        }
    }
    public class FileUploadController : Controller
    {
        private readonly RankContext _context;
        public FileUploadController(RankContext context) => _context = context;
        public int dataId;
        string filePath;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("FileUpload")]
        public async Task<IActionResult> Index(List<IFormFile> files)
        {
            var size = files.Sum(f => f.Length);

            var filePaths = new List<string>();
            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    var tempPath = Path.GetTempFileName();
                    filePath = Path.Combine(Path.GetTempPath(), formFile.FileName);
                    filePaths.Add(filePath);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                }
            }

            CsvParser(Path.Combine(Path.GetTempPath(), filePath));

            return RedirectToAction("Index", "Rankings", new { area = "" });
        }
        // CSV parser
        public const string SamsungFieldStart = "com.samsung.health.exercise.";

        public const string FieldDuration = "Samsung_duration";
        public const string FieldDistance = "Samsung_distance";
        public const string FieldMeanSpeed = "Samsung_mean_speed";
        public const string FieldType = "Samsung_exercise_type";

        public async void CsvParser(string path)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                GetDynamicPropertyName = GetGoodPropertyName
            };

            var scores = new List<ExcerciseScore>();

            using (var streamReader = new StreamReader(path))
            {
                using (var csvReader = new CsvReader(streamReader, csvConfig))
                {
                    var dynamicRecords = csvReader.GetRecords<dynamic>();
                    var records = RecordsAsStringDictionary(dynamicRecords);

                    foreach (var record in records)
                    {
                        var type = record[FieldType];
                        if (type != CsvFieldTypes.Walking)
                        {
                            continue;
                        }

                        var score = new ExcerciseScore
                        {
                            Type = ExcerciseType.Walking,
                        };

                        double distance;
                        double meanSpeed;

                        if (!double.TryParse(record[FieldDistance], out distance) || !double.TryParse(record[FieldMeanSpeed], out meanSpeed))
                        {
                            continue;
                        }
                        distance = distance / 1000; // converts to km
                        double calculatedScore = distance * (meanSpeed * distance) * 3.5;
                        score.Score = Math.Round(calculatedScore, 2);
                        scores.Add(score);

                    }
                }

                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                    var userId = userIdClaim?.Value;
                    var UserEmail = User.Identity.Name;

                    var dbRanking = _context.Ranking.FirstOrDefault(rank => UserEmail == rank.Username);
                    int totalScore = scores.Sum(x => Convert.ToInt32(x.Score));
                    if (dbRanking == null)
                    {
                        _context.Add(
                        new Ranking
                        {
                            Score = totalScore,
                            Username = UserEmail
                        });
                        _context.SaveChanges();
                    }
                    else
                    {
                        dbRanking.Score = totalScore;
                        _context.Update(dbRanking);
                        _context.SaveChanges();
                    }
                    RedirectToAction("Index", "Rankings", new { area = "" });

                }
                else
                {
                    RedirectToAction("Index", "Rankings", new { area = "" });
                }
            }
        }

        public static string GetGoodPropertyName(GetDynamicPropertyNameArgs args)
        {
            var name = args.Context.Reader.HeaderRecord![args.FieldIndex];
            return name.Replace(SamsungFieldStart, "Samsung_");
        }

        public static List<Dictionary<string, string>> RecordsAsStringDictionary(IEnumerable<dynamic>? records)
        {
            var recordList = new List<Dictionary<string, string>>();
            if (records is null)
            {
                return recordList;
            }

            foreach (var record in records)
            {
                var recordValues = new Dictionary<string, string>();
                var values = (IDictionary<string, object>)record;
                foreach (var value in values)
                {
                    recordValues.Add(value.Key, (string)value.Value);
                }
                recordList.Add(recordValues);
            }

            return recordList;
        }
    }
}
