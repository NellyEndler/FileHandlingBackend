namespace FileHandlingBackend.Cryptography
{
    public interface IHasher
    {
        string CreateHash(string password);
        bool ValidatePassword(string password, string correctHash);
        bool SlowEquals(byte[] a, byte[] b);
    }
}
