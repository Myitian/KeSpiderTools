using KeSpider.API;

namespace KeSpider;

public readonly struct PostInfo(string id, string user, string service, string domain) : IEquatable<PostInfo>
{
    public readonly string ID = id;
    public readonly string User = user;
    public readonly string Service = service;
    public readonly string Domain = domain;

    public PostInfo(PostsResult post, string domain) : this(post.ID, post.User, post.Service, domain) { }

    public void Deconstruct(out string id, out string user, out string service, out string domain)
    {
        id = ID;
        user = User;
        service = Service;
        domain = Domain;
    }
    public bool Equals(PostInfo other)
    {
        return ID == other.ID && User == other.User && Service == other.Service;
    }
    public override bool Equals(object? obj)
    {
        return obj is PostInfo info && Equals(info);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(ID, User, Service);
    }
    public static bool operator ==(PostInfo left, PostInfo right)
    {
        return left.Equals(right);
    }
    public static bool operator !=(PostInfo left, PostInfo right)
    {
        return !left.Equals(right);
    }
}
