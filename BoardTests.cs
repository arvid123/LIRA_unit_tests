using LIRA;

namespace LIRA_unit_tests
{
    [TestClass]
    public class BoardTests
    {
        private Board? board;

        [TestInitialize()]
        public void Startup()
        {
            board = new Board();
        }

        [DataTestMethod]
        [DataRow(IssueType.TASK)]
        [DataRow(IssueType.FEATURE)]
        [DataRow(IssueType.EPIC)]
        public void Given_NoIssues_When_IssueAdded_Then_IssueCanBeRetrieved(IssueType type)
        {
            Guid issueID = board.AddIssue("test issue", type);
            Issue issue = board.GetIssue(issueID);

            Assert.AreEqual("test issue", issue.title);
            Assert.AreEqual(type, issue.type);
            Assert.AreEqual(IssueState.TODO, issue.state);
        }

        [TestMethod]
        public void Given_OneIssue_When_IssueRemoved_Then_IssueListEmpty() 
        {
            Guid issueID = board.AddIssue("test issue", IssueType.TASK);
            board.RemoveIssue(issueID);

            List<Issue> issues = board.GetIssues();
            Assert.AreEqual(0, issues.Count);
        }

        [TestMethod]
        public void Given_OneAssignment_When_FilteringForAssignment_Then_CorrectIssueReturns() 
        {
            Guid userID = board.AddUser("testuser");
            Guid issueID = board.AddIssue("test issue", IssueType.EPIC);
            board.AssignUser(userID, issueID);

            Guid wrongIssueID = board.AddIssue("should not be found when filtering", IssueType.FEATURE);
            List<Issue> filteredIssues = board.GetIssues(userID: userID);

            Assert.IsTrue(filteredIssues.Select(x => x.title).Contains("test issue"));
            Assert.IsFalse(filteredIssues.Select(x => x.title).Contains("should not be found when filtering"));
        }

        [TestMethod]
        public void Given_SeveralIssueTypes_When_FilteringForTwoTypes_Then_CorrectIssuesReturn()
        {
            Guid issueID1 = board.AddIssue("Epic", IssueType.EPIC);
            Guid issueID2 = board.AddIssue("Epic2", IssueType.EPIC);
            Guid issueID3 = board.AddIssue("Feature", IssueType.FEATURE);
            Guid issueID4 = board.AddIssue("Feature2", IssueType.FEATURE);
            Guid issueID5 = board.AddIssue("Task", IssueType.TASK);
            Guid issueID6 = board.AddIssue("Task2", IssueType.TASK);

            List<IssueType> types = new List<IssueType>();
            types.Add(IssueType.FEATURE);
            types.Add(IssueType.TASK);
            List<Issue> issues = board.GetIssues(issueTypes: types);

            Assert.IsTrue(issues.Select(x => x.type).Contains(IssueType.FEATURE));
            Assert.IsTrue(issues.Select(x => x.type).Contains(IssueType.TASK));
            Assert.IsFalse(issues.Select(x => x.type).Contains(IssueType.EPIC));
        }

        [TestMethod]
        public void Given_DateInterval_When_FilteringByDate_Then_CorrectIssueReturns()
        {
            board.AddIssue("test", IssueType.TASK);
            board.AddIssue("test2", IssueType.FEATURE);

            List<Issue> issues1 = board.GetIssues(startDate: DateTime.Now.AddDays(-1), endDate: DateTime.Now.AddDays(1));
            List<Issue> issues2 = board.GetIssues(startDate: DateTime.Now.AddDays(-1), endDate: DateTime.Now.AddDays(-0.5));
            List<Issue> issues3 = board.GetIssues(endDate: DateTime.Now.AddDays(1));
            List<Issue> issues4 = board.GetIssues(endDate: DateTime.Now.AddDays(-1));
            List<Issue> issues5 = board.GetIssues(startDate: DateTime.Now.AddDays(-1));
            List<Issue> issues6 = board.GetIssues(startDate: DateTime.Now.AddDays(1));

            Assert.AreEqual(2, issues1.Count);
            Assert.AreEqual(0, issues2.Count);
            Assert.AreEqual(2, issues3.Count);
            Assert.AreEqual(0, issues4.Count);
            Assert.AreEqual(2, issues5.Count);
            Assert.AreEqual(0, issues6.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Given_InvalidTimeInterval_When_FilteringByDate_Then_ThrowsException()
        {
            board.GetIssues(startDate: DateTime.Now.AddDays(1), endDate: DateTime.Now.AddDays(-1));
        }

        [TestMethod]
        public void Given_IssuesInSeveralStates_When_FilteringByState_Then_CorrectIssuesReturn()
        {
            Guid issue1 = board.AddIssue("test1", IssueType.TASK);
            Guid issue2 = board.AddIssue("test2", IssueType.TASK);
            Guid issue3 = board.AddIssue("test3", IssueType.TASK);
            Guid issue4 = board.AddIssue("test4", IssueType.TASK);
            Guid issue5 = board.AddIssue("test5", IssueType.TASK);
            Guid issue6 = board.AddIssue("test6", IssueType.TASK);
            board.SetIssueState(issue1, IssueState.TODO);
            board.SetIssueState(issue2, IssueState.TODO);
            board.SetIssueState(issue3, IssueState.IN_PROGRESS);
            board.SetIssueState(issue4, IssueState.IN_PROGRESS);
            board.SetIssueState(issue5, IssueState.DONE);
            board.SetIssueState(issue6, IssueState.DONE);

            List<Issue> issues = board.GetIssues(state: IssueState.IN_PROGRESS);

            Assert.IsTrue(issues.Select(x => x.title).Contains("test3"));
            Assert.IsTrue(issues.Select(x => x.title).Contains("test4"));
            Assert.IsFalse(issues.Select(x => x.title).Contains("test1"));
            Assert.IsFalse(issues.Select(x => x.title).Contains("test2"));
            Assert.IsFalse(issues.Select(x => x.title).Contains("test5"));
            Assert.IsFalse(issues.Select(x => x.title).Contains("test6"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Given_ChildrenNotDone_When_ParentSetToDone_Then_ExceptionThrown()
        {
            Guid parent = board.AddIssue("parent", IssueType.FEATURE);
            Guid child = board.AddIssue("child", IssueType.TASK);
            board.SetParentIssue(child, parent);

            board.SetIssueState(parent, IssueState.DONE);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Given_GrandChildrenNotDone_When_ParentSetToDone_Then_ExceptionThrown()
        {
            Guid parent = board.AddIssue("parent", IssueType.EPIC);
            Guid child = board.AddIssue("child", IssueType.FEATURE);
            Guid grandchild = board.AddIssue("grandchild", IssueType.TASK);
            board.SetParentIssue(child, parent);
            board.SetParentIssue(grandchild, child);

            board.SetIssueState(grandchild, IssueState.DONE);
            board.SetIssueState(child, IssueState.DONE);
            board.SetIssueState(grandchild, IssueState.IN_PROGRESS);
            board.SetIssueState(parent, IssueState.DONE);
        }

        [TestMethod]
        public void Given_UserAssignedToIssue_When_RemovingUser_Then_IssueDoesNotShowWhenFilteringForUser()
        {
            Guid userID = board.AddUser("testuser");
            Guid issue = board.AddIssue("testissue", IssueType.TASK);
            board.AssignUser(userID, issue);

            List<Issue> issuesBefore = board.GetIssues(userID: userID);
            board.RemoveUser(userID);
            List<Issue> issuesAfter = board.GetIssues(userID: userID);

            Assert.IsTrue(issuesBefore.Select(x => x.title).Contains("testissue"));
            Assert.IsTrue(issuesAfter.Count == 0);
        }

        [TestMethod]
        public void Given_UserRemoved_When_ListingUsers_UserIsGone()
        {
            Guid userID = board.AddUser("testuser");
            Guid userID2 = board.AddUser("testuser2");
            
            board.RemoveUser(userID);
            List<User> users = board.GetUsers();

            Assert.IsFalse(users.Select(x => x.name).Contains("testuser"));
            Assert.IsTrue(users.Select(x => x.name).Contains("testuser2"));

        }
    }
}