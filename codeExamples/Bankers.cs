using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        Console.Write("Number of processes: ");
        int n = int.Parse(Console.ReadLine() ?? "0");

        Console.Write("Number of resource types: ");
        int m = int.Parse(Console.ReadLine() ?? "0");

        int[,] alloc = new int[n, m];
        int[,] max   = new int[n, m];
        int[]  avail = new int[m];

        Console.WriteLine("Enter allocation matrix:");
        for (int i = 0; i < n; i++) ReadRow(alloc, i, m, $"P{i + 1}: ");

        Console.WriteLine("Enter maximum demand matrix:");
        for (int i = 0; i < n; i++) ReadRow(max, i, m, $"P{i + 1}: ");

        Console.Write("Enter available resources: ");
        avail = ParseLine(m);

        // Need[i,j] = Max[i,j] - Alloc[i,j]
        int[,] need = new int[n, m];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < m; j++)
                need[i, j] = max[i, j] - alloc[i, j];

        // Banker's safety check
        bool[] finished = new bool[n];
        int[]  work     = (int[])avail.Clone();
        var    sequence = new List<int>();

        bool progress = true;
        while (progress && sequence.Count < n)
        {
            progress = false;
            for (int i = 0; i < n; i++)
            {
                if (finished[i]) continue;

                // Can process i finish with current work vector?
                bool ok = true;
                for (int j = 0; j < m; j++)
                    if (need[i, j] > work[j]) { ok = false; break; }

                if (ok)
                {
                    // Process finishes - release its allocation
                    for (int j = 0; j < m; j++) work[j] += alloc[i, j];
                    finished[i] = true;
                    sequence.Add(i);
                    progress = true;
                }
            }
        }

        if (sequence.Count == n)
        {
            Console.WriteLine("Safe Sequence: " +
                string.Join(" -> ", sequence.Select(i => $"P{i + 1}")));
            Console.WriteLine("System is in a safe state.");
        }
        else
        {
            Console.WriteLine("System is NOT in a safe state. No safe sequence exists.");
        }
    }

    // Read m space-separated ints from one line
    static int[] ParseLine(int m)
    {
        var parts = (Console.ReadLine() ?? "").Split(' ',
            StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != m)
            throw new ArgumentException($"Expected {m} values, got {parts.Length}");
        return parts.Select(int.Parse).ToArray();
    }

    static void ReadRow(int[,] matrix, int row, int m, string prompt)
    {
        Console.Write(prompt);
        var values = ParseLine(m);
        for (int j = 0; j < m; j++) matrix[row, j] = values[j];
    }
}
