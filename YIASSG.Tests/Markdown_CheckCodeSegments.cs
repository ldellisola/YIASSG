using System.IO;
using FluentAssertions;
using YIASSG.Exceptions;
using YIASSG.Models;
using YIASSG.Tests.Utils;
using NUnit.Framework;

namespace YIASSG.Tests;

public class Markdown_CheckCodeSegments
{
    private static Markdown? _md;

    [SetUp]
    public void Setup()
    {
        _md = new Markdown(new AppSettings());
    }

    [TestCase("./Resources/CodeSegmentExamples/Invalid/simple.md")]
    [TestCase("./Resources/CodeSegmentExamples/Invalid/simpleWithTags.md")]
    [TestCase("./Resources/CodeSegmentExamples/Invalid/multiple.md")]
    [TestCase("./Resources/CodeSegmentExamples/Invalid/multipleWithTags.md")]
    [TestCase("./Resources/CodeSegmentExamples/Invalid/atDifferentIndentation.md")]
    public void WhenUsingInvalidFile_ItThrowsInvalidCodeSegmentException(string filename)
    {
        // Arrange
        var inputText = new FileInfo(filename).OpenText().ReadToEnd();

        // Act
        var ex = Catch.Exception(() => _md!.CheckCodeSegments(inputText, filename));

        // Assert
        ex.Should().NotBeNull().And.BeOfType<InvalidCodeSegmentException>();
    }

    [TestCase("./Resources/CodeSegmentExamples/Valid/complex.md")]
    public void WhenUsingValidFile_ItDoesNotThrowAnException(string filename)
    {
        // Arrange
        var inputText = new FileInfo(filename).OpenText().ReadToEnd();

        // Act
        var ex = Catch.Exception(() => _md!.CheckCodeSegments(inputText, filename));

        // Assert
        ex.Should().BeNull();
    }
}