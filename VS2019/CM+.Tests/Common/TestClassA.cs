class TestClassA
{
    /// <summary>
    /// The test1
    /// </summary>
    int test1;

    /// <summary>
    /// The test2
    /// </summary>

    int test2;

    void Test()
    {
        var linesWithCode = root.DescendantTokens()
                                .Where(x => x.HasLeadingTrivia &&
                                            x.LeadingTrivia
                                            .Select(x => x)
                                            .Any(y => y.IsKind(SyntaxKind.WhitespaceTrivia)));

        System.Console.WriteLine("");
        var ttt = ""
    .Select(x => x.Select(y => y)
                 .Select(y => y)
                 .Select(y => y))
    .Select(x => x)
    .Select(x => x)
    .Select(x => x)
    .Select(x => x);

        if (true)
        {
            var ttt2 = ""
                       .Select(x => x)
                       // dfs
                       .Select(x => x)
                       .Select(x => x)
                       .Select(x => x)
                       .Select(x => x);
        }

        Console.WriteLine(""
        .Select(x => x));
    }
}