using EaGpt;
using Xunit;

namespace EaGpt.Tests
{
    public class DiagramCreationIntentTests
    {
        [Fact]
        public void AddToThisView_IsNotBrandNew()
        {
            Assert.False(DiagramCreationIntent.UserAskedForBrandNewView("Add a BusinessActor to this view"));
            Assert.False(DiagramCreationIntent.UserAskedForBrandNewView("put a process on the current diagram"));
        }

        [Fact]
        public void AddElementOnly_IsNotBrandNew()
        {
            Assert.False(DiagramCreationIntent.UserAskedForBrandNewView("Add a BusinessActor called Customer"));
        }

        [Fact]
        public void ExplicitNewDiagram_IsBrandNew()
        {
            Assert.True(DiagramCreationIntent.UserAskedForBrandNewView("Create a new diagram for HR"));
            Assert.True(DiagramCreationIntent.UserAskedForBrandNewView("add a new view"));
            Assert.True(DiagramCreationIntent.UserAskedForBrandNewView("add a diagram that describes recruitment"));
        }

        [Fact]
        public void DropsSpuriousDiagramWhenViewIsOpen()
        {
            var result = new ArchiMateLlmResult();
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "Unwanted extra view" };
            Assert.True(DiagramCreationIntent.TryDropUnwantedDiagram(result, "Add a BusinessActor to this view", hasOpenDiagram: true));
            Assert.Null(result.Diagram);
        }

        [Fact]
        public void KeepsDiagramWhenUserAskedForNewView()
        {
            var result = new ArchiMateLlmResult();
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "Recruitment Process" };
            Assert.False(DiagramCreationIntent.TryDropUnwantedDiagram(result, "Create a new diagram for HR", hasOpenDiagram: true));
            Assert.Equal("Recruitment Process", result.Diagram!.Name);
        }

        [Fact]
        public void KeepsDiagramWhenNoViewIsOpen()
        {
            var result = new ArchiMateLlmResult();
            result.Diagram = new ArchiMateLlmResult.DiagramSpec { Name = "Landscape" };
            Assert.False(DiagramCreationIntent.TryDropUnwantedDiagram(result, "Add a BusinessActor called Customer", hasOpenDiagram: false));
            Assert.NotNull(result.Diagram);
        }
    }
}
