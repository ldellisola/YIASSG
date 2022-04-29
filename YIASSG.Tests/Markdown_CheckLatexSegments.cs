using System.IO;
using FluentAssertions;
using YIASSG.Exceptions;
using YIASSG.Models;
using YIASSG.Tests.Utils;
using NUnit.Framework;

namespace YIASSG.Tests;

public class Markdown_CheckLatexSegments
{
    private Markdown? _md;

    [SetUp]
    public void SetUp()
    {
        _md = new Markdown(new AppSettings());
    }


    [TestCase("./Resources/LatexSegmentExamples/Invalid/simple.md")]
    [TestCase("./Resources/LatexSegmentExamples/Invalid/multiple.md")]
    [TestCase("./Resources/LatexSegmentExamples/Invalid/atDifferentIndentation.md")]
    public void WhenUsingInvalidFile_ItThrowsInvalidLatexSegmentException(string filename)
    {
        // Arrange
        var inputText = new FileInfo(filename).OpenText().ReadToEnd();

        // Act
        var ex = Catch.Exception(() => _md!.CheckLatexSegments(inputText, filename));

        // Assert
        ex.Should().NotBeNull().And.BeOfType<InvalidLatexSegmentException>();
    }

    [TestCase("./Resources/LatexSegmentExamples/Valid/complex.md")]
    public void WhenUsingValidFile_ItDoesNotThrowAnException(string filename)
    {
        // Arrange
        var inputText = new FileInfo(filename).OpenText().ReadToEnd();

        // Act
        var ex = Catch.Exception(() => _md!.CheckLatexSegments(inputText, filename));

        // Assert
        ex.Should().BeNull();
    }
}