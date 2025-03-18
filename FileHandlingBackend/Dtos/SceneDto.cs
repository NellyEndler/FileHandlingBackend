namespace FileHandlingBackend.Dtos
{
    public class SceneDto
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string JsonUrl { get; set; }
    }

    public class CreateSceneDto
    {
        public string Name { get; set; }
        public IFormFile? SceneFile { get; set; }
    }
}
