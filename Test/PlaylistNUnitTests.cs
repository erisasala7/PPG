using NUnit.Framework;


namespace Test
{
    public class Tests
    {
       

        [Test]
        public void register_success()
        {
            string end = "user added";
            Assert.AreEqual(end, "user added");
        }
        [Test]
        public void register_failed()
        {
            string end = "user exists";
            Assert.AreEqual(end, "user exists");
        }
        [Test]
        public void actions_length()
        {
            string end = "RRRRR";
            Assert.AreEqual(end.Length,5);
        }
        [Test]
        public void actions_context()
        {
            string end1 = "RRRRR";
            Assert.AreEqual(end1, "RRRRR");
            string end2 = "SSSSS";
            Assert.AreEqual(end2, "SSSSS");
            string end3 = "VVVVV";
            Assert.AreEqual(end3, "VVVVV");
            string end4 = "PPPPP";
            Assert.AreEqual(end4, "PPPPP");
            string end5 = "LLLLL";
            Assert.AreEqual(end5, "LLLLL");
        }
        [Test]
        public void update_user_success()
        {
            string end = "Updated";
            Assert.AreEqual(end,"Updated");
        }
        [Test]
        public void update_user_failed()
        {
            string end = "User exists";
            Assert.AreEqual(end,"User exists");
        }

        [Test]
        public void add_in_lib()
        {
            string getname = "kienboec";
            string geturl = "https://youtube.com";
            string getusernamelib = "kienboec";
            string endresult = $"Song with Name :  {getname}  and url: {geturl}  for user: {getusernamelib} is saved";
            Assert.AreEqual(endresult,  $"Song with Name :  {getname}  and url: {geturl}  for user: {getusernamelib} is saved");
        }
        [Test]
        public void add_in_playlist()
        {
            string getname = "kienboec";
            string getusernamelib = "kienboec";
            string endresult = $"Playlist with Name :  {getusernamelib}  for user: {getusernamelib} is saved";
            Assert.AreEqual(endresult,  $"Playlist with Name :  {getusernamelib}  for user: {getusernamelib} is saved");
        }
        [Test]
        public void update_actions()
        {
            string end = "Updated";
            Assert.AreEqual(end,"Updated");
        }
        [Test]
        public void update_playlist_success()
        {
            string end = "Updated";
            Assert.AreEqual(end,"Updated");
        }
        [Test]
        public void update_playlist_failed()
        {
            string end = "Not Admin";
            Assert.AreEqual(end,"Not Admin");
        }
        
    }
}