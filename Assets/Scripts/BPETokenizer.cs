using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

public class BPETokenizer
{
    private Dictionary<string, int> encoder;
    private Dictionary<string, int> bpeRanks;
    private Dictionary<string, string> cache = new Dictionary<string, string>();

    public BPETokenizer(string vocabJson, string mergesTxt)
    {
        // 1. 解析 vocab.json (使用 Regex 避免依赖 Newtonsoft)
        encoder = new Dictionary<string, int>();
        var matches = Regex.Matches(vocabJson, "\"([^\"]+)\":\\s*(\\d+)");
        foreach (Match match in matches)
        {
            // Unescape unicode characters if necessary
            string key = Regex.Unescape(match.Groups[1].Value);
            if (!encoder.ContainsKey(key)) encoder.Add(key, int.Parse(match.Groups[2].Value));
        }

        // 2. 解析 merges.txt
        bpeRanks = new Dictionary<string, int>();
        string[] lines = mergesTxt.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        // 跳过第一行 (通常是 version info)
        int startLine = lines[0].StartsWith("#") ? 1 : 0;
        for (int i = startLine; i < lines.Length; i++)
        {
            string line = lines[i];
            // Merges format: "l e" -> rank i
            if (!bpeRanks.ContainsKey(line)) bpeRanks.Add(line, i);
        }
    }

    public List<int> Encode(string text)
    {
        List<int> bpeTokens = new List<int>();
        // RoBERTa start token (<s> = 0)
        bpeTokens.Add(0);

        // 简单预处理：转小写 (RoBERTa 其实是大小写敏感的，但为了容错建议保留原样或根据模型定)
        // Twitter-RoBERTa 通常保留大小写，但这里为了简单按空格分
        string[] words = text.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string word in words)
        {
            // RoBERTa 特性：单词前加前缀空格 (Ġ)
            // 在 byte-level BPE 中，这通常映射为 \u0120
            // 这里简化处理，直接用 vocab 里的 key
            string token = "Ġ" + word;

            foreach (string bpeToken in BPE(token))
            {
                if (encoder.ContainsKey(bpeToken))
                    bpeTokens.Add(encoder[bpeToken]);
                else
                    bpeTokens.Add(3); // <unk> = 3
            }
        }

        // RoBERTa end token (</s> = 2)
        bpeTokens.Add(2);
        return bpeTokens;
    }

    private IEnumerable<string> BPE(string token)
    {
        if (cache.ContainsKey(token)) return cache[token].Split(' ');

        // 将单词拆分为字符列表
        List<string> word = new List<string>();
        for (int i = 0; i < token.Length; i++) word.Add(token[i].ToString());

        HashSet<string> pairs = GetPairs(word);

        if (pairs.Count == 0) return word;

        while (true)
        {
            // 找到 rank 最小的 pair (即在 merges.txt 中最靠前的)
            string bigram = null;
            int minRank = int.MaxValue;

            foreach (string pair in pairs)
            {
                // 构造 merges.txt 中的格式 "A B"
                string[] parts = pair.Split(',');
                string check = parts[0] + " " + parts[1];

                if (bpeRanks.ContainsKey(check))
                {
                    int rank = bpeRanks[check];
                    if (rank < minRank)
                    {
                        minRank = rank;
                        bigram = pair;
                    }
                }
            }

            if (bigram == null) break;

            string[] bestParts = bigram.Split(',');
            string first = bestParts[0];
            string second = bestParts[1];
            List<string> newWord = new List<string>();
            int i = 0;

            while (i < word.Count)
            {
                int j = word.IndexOf(first, i);
                if (j == -1)
                {
                    for (int k = i; k < word.Count; k++) newWord.Add(word[k]);
                    break;
                }

                for (int k = i; k < j; k++) newWord.Add(word[k]);
                i = j;

                if (i < word.Count - 1 && word[i] == first && word[i + 1] == second)
                {
                    newWord.Add(first + second);
                    i += 2;
                }
                else
                {
                    newWord.Add(word[i]);
                    i += 1;
                }
            }

            word = newWord;
            if (word.Count == 1) break;
            pairs = GetPairs(word);
        }

        string result = string.Join(" ", word);
        cache[token] = result;
        return word;
    }

    private HashSet<string> GetPairs(List<string> word)
    {
        HashSet<string> pairs = new HashSet<string>();
        for (int i = 0; i < word.Count - 1; i++)
        {
            pairs.Add(word[i] + "," + word[i + 1]);
        }
        return pairs;
    }
}