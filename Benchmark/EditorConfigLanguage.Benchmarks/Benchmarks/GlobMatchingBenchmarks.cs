using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using EditorConfigLanguage.Benchmarks.Support;

namespace EditorConfigLanguage.Benchmarks.Benchmarks
{
    /// <summary>Measures compilation and matching of increasingly complex glob patterns.</summary>
    [MemoryDiagnoser]
    public class GlobMatchingBenchmarks
    {
        private static readonly Dictionary<string, string> Patterns = new()
        {
            ["simple"] = "*.cs",
            ["wide-alternation"] = "{a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z}*.{cs,vb,fs,ts,js,py,rb,go,rs,java,kt,swift,cpp,c,h}",
            ["nested-choice"] = "{src,{lib,{core,{shared,{common,internal}}}}}/**/*.{cs,csx}",
            ["number-range-and-wildcards"] = "**/v{1..999}/{a,b,c}*/**/?{x,y,z}.config",
        };

        private static readonly string[] CandidatePaths = GenerateCandidatePaths(500);

        [ParamsSource(nameof(PatternNames))]
        public string PatternName { get; set; }

        public IEnumerable<string> PatternNames => Patterns.Keys;

        [Benchmark]
        public bool CompilePattern()
            => ProductionApi.CreateMatcher(Patterns[PatternName]) != null;

        [Benchmark]
        public int MatchAgainstManyPaths()
        {
            object matcher = ProductionApi.CreateMatcher(Patterns[PatternName]);
            if (matcher is null)
                return 0;

            int matchCount = 0;
            foreach (string path in CandidatePaths)
            {
                if (ProductionApi.IsMatch(matcher, path))
                    matchCount++;
            }

            return matchCount;
        }

        private static string[] GenerateCandidatePaths(int count)
        {
            string[] extensions = ["cs", "vb", "json", "yml", "config", "txt", "md"];
            string[] segments = ["src", "lib", "core", "shared", "common", "internal", "v12", "v345", "a1", "b2", "c3", "x", "y", "z"];

            var random = new Random(Seed: 42);
            var paths = new string[count];

            for (int i = 0; i < count; i++)
            {
                int depth = 1 + random.Next(6);
                var parts = new string[depth];
                for (int d = 0; d < depth; d++)
                {
                    parts[d] = segments[random.Next(segments.Length)];
                }

                string extension = extensions[random.Next(extensions.Length)];
                paths[i] = "/" + string.Join("/", parts) + $"/file{i}.{extension}";
            }

            return paths;
        }
    }
}
