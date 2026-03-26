using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class RobertaTokenizer
{
    private Dictionary<string, int> encoder;
    private Dictionary<string, int> bpeRanks;
    private Dictionary<string, string> cache = new Dictionary<string, string>();
    private const string SpaceSymbol = "Ġ"; // RoBERTa special character

    public RobertaTokenizer(TextAsset vocabFile, TextAsset mergesFile)
    {
        // analyze vocab.json
        encoder = new Dictionary<string, int>();
        var matches = Regex.Matches(vocabFile.text, "\"([^\"]+)\":\\s*(\\d+)");
        foreach (Match match in matches)
        {
            string key = Regex.Unescape(match.Groups[1].Value);
            if (!encoder.ContainsKey(key)) encoder.Add(key, int.Parse(match.Groups[2].Value));
        }

        // analyze merges.txt
        bpeRanks = new Dictionary<string, int>();
        string[] lines = mergesFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int startLine = lines[0].StartsWith("#") ? 1 : 0;
        for (int i = startLine; i < lines.Length; i++)
        {
            // "char1 char2" format
            if (!bpeRanks.ContainsKey(lines[i])) bpeRanks.Add(lines[i], i);
        }
    }

    public List<int> Encode(string text)
    {
        List<int> tokens = new List<int>();
        tokens.Add(0); // <s> Start Token

        // Preprocessing
        string cleanText = text.Trim();

        // split words by space
        string[] words = cleanText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length; i++)
        {
            string word = (i == 0 ? "" : SpaceSymbol) + words[i];

            foreach (string token in BPE(word))
            {
                tokens.Add(encoder.ContainsKey(token) ? encoder[token] : 3); // 3 = <unk>
            }
        }

        tokens.Add(2); // </s> End Token
        return tokens;
    }

    private IEnumerable<string> BPE(string token)
    {
        if (cache.ContainsKey(token)) return cache[token].Split(' ');

        List<string> word = new List<string>();
        for (int i = 0; i < token.Length; i++) word.Add(token[i].ToString());

        while (word.Count > 1)
        {
            int minRank = int.MaxValue;
            string bestBigram = null;

            for (int i = 0; i < word.Count - 1; i++)
            {
                string pair = word[i] + " " + word[i + 1];
                if (bpeRanks.ContainsKey(pair))
                {
                    int rank = bpeRanks[pair];
                    if (rank < minRank) { minRank = rank; bestBigram = pair; }
                }
            }

            if (bestBigram == null) break;

            string[] parts = bestBigram.Split(' ');
            List<string> newWord = new List<string>();
            for (int i = 0; i < word.Count; i++)
            {
                if (i < word.Count - 1 && word[i] == parts[0] && word[i + 1] == parts[1])
                {
                    newWord.Add(parts[0] + parts[1]);
                    i++;
                }
                else newWord.Add(word[i]);
            }
            word = newWord;
        }

        string result = string.Join(" ", word);
        cache[token] = result;
        return word;
    }
}
