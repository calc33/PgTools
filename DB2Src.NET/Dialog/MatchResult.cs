using System.Text.RegularExpressions;

namespace Db2Source
{
    public struct MatchResult
    {
        public int Index { get; private set; }
        public int Length { get; private set; }
        public bool Success
        {
            get
            {
                return (Index != -1) && (0 < Length);
            }
        }
        public MatchResult(int start, int length)
        {
            Index = start;
            Length = length;
        }
        public MatchResult(Match match)
        {
            Index = match.Index;
            Length = match.Length;
        }
        public static readonly MatchResult Unmatched = new MatchResult(-1, 0);
    }
}
