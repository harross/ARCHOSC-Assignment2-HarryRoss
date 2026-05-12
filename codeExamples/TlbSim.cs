using System;
using System.Collections.Generic;

class TLBEntry
{
    public int VPN;
    public int PPN;
}

class TLB
{
    private List<TLBEntry> entries = new List<TLBEntry>();
    private const int capacity = 4;
    public int Hits = 0;
    public int Misses = 0;

    public int? Lookup(int vpn)
    {
        foreach (var entry in entries)
        {
            if (entry.VPN == vpn) { Hits++; return entry.PPN; }
        }
        Misses++;
        return null; // TLB miss
    }

    public void Insert(int vpn, int ppn)
    {
        if (entries.Count >= capacity) entries.RemoveAt(0);  // FIFO eviction
        entries.Add(new TLBEntry { VPN = vpn, PPN = ppn });
    }

    public double HitRatio => (Hits + Misses) == 0 ? 0 : (double)Hits / (Hits + Misses);
}

class Program
{
    static void Main()
    {
        // Mock page table - VPN -> PPN
        var pageTable = new Dictionary<int, int>
        {
            { 0, 5 }, { 1, 2 }, { 2, 7 }, { 3, 1 },
            { 4, 6 }, { 5, 3 }, { 6, 0 }, { 7, 4 }
        };

        var tlb = new TLB();
        int[] accesses = { 0, 1, 0, 2, 3, 0, 1, 4, 5, 0, 1, 6, 7, 0, 1 };

        Console.WriteLine("VPN  | Result | PPN");
        Console.WriteLine("-----+--------+----");
        foreach (int vpn in accesses)
        {
            int? ppn = tlb.Lookup(vpn);
            if (ppn == null)
            {
                int realPPN = pageTable[vpn];
                tlb.Insert(vpn, realPPN);
                Console.WriteLine($"{vpn,-4} | MISS   | {realPPN}");
            }
            else
            {
                Console.WriteLine($"{vpn,-4} | HIT    | {ppn}");
            }
        }

        Console.WriteLine($"\nHits: {tlb.Hits}, Misses: {tlb.Misses}");
        Console.WriteLine($"Hit ratio: {tlb.HitRatio:P1}");
    }
}
