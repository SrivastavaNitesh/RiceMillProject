using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RiceMillProject.BAL;
using RiceMillProject.Models;

namespace RiceMillProject.Controllers
{
    public class PostController : Controller
    {
        private readonly PostBAL _postBal;

        public PostController(IConfiguration configuration)
        {
            _postBal = new PostBAL(configuration);
        }

        public IActionResult Index()
        {
            var posts = _postBal.GetAllPosts();
            return View(posts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Post { IsActive = true });
        }

        [HttpPost]
        public IActionResult Create(Post post)
        {
            if (ModelState.IsValid)
            {
                _postBal.AddPost(post);
                return RedirectToAction("Index");
            }
            return View(post);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var post = _postBal.GetAllPosts().Find(p => p.PostId == id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        [HttpPost]
        public IActionResult Edit(Post post)
        {
            if (ModelState.IsValid)
            {
                _postBal.UpdatePost(post);
                return RedirectToAction("Index");
            }
            return View(post);
        }
        
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var post = _postBal.GetAllPosts().Find(p => p.PostId == id);
            if (post != null)
            {
                post.IsActive = false; // Soft delete
                _postBal.UpdatePost(post);
            }
            return RedirectToAction("Index");
        }
    }
}
