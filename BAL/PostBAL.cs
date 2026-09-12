using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using RiceMillProject.DAL;
using RiceMillProject.Models;

namespace RiceMillProject.BAL
{
    public class PostBAL
    {
        private readonly PostDAL _postDal;

        public PostBAL(IConfiguration configuration)
        {
            _postDal = new PostDAL(configuration);
        }

        public List<Post> GetAllPosts()
        {
            return _postDal.GetAllPosts();
        }

        public int AddPost(Post post)
        {
            return _postDal.InsertPost(post);
        }

        public bool UpdatePost(Post post)
        {
            return _postDal.UpdatePost(post);
        }
    }
}
