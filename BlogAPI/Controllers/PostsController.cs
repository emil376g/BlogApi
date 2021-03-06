﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogAPI.Models;
using Blog.Web.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly BlogAPIContext _context;

        public PostsController(BlogAPIContext context)
        {
            _context = context;
        }

        // GET: api/Posts
        [HttpGet]
        public IEnumerable<PostDTO> GetPosts()
        {

            return AutoMapper.Mapper.Map<IEnumerable<Post>, IEnumerable<PostDTO>>((from Userinfo in _context.UserInfos
                                                                                   join post in _context.Posts on Userinfo equals post.PostingUser
                                                                                   select new Post()
                                                                                   {
                                                                                       Content = post.Content,
                                                                                       DateOfPost = post.DateOfPost,
                                                                                       PostId = post.PostId,
                                                                                       ImageUrl = post.ImageUrl,
                                                                                       PostingUser = new UserInfo() { Name = Userinfo.Name, NumberOfComments = Userinfo.NumberOfComments, NumberOfPosts = Userinfo.NumberOfPosts, Posts = Userinfo.Posts, ProfilPictureUrl = Userinfo.ProfilPictureUrl, RegisterDate = Userinfo.RegisterDate, UserInfoID = Userinfo.UserInfoID, Username = Userinfo.Username },
                                                                                       Title = post.Title
                                                                                   }).ToList());
        }

        // GET: api/Posts/5
        [HttpGet("{id}")]
        public IActionResult GetPost([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var posts = from Userinfo in _context.UserInfos
                        join comments in _context.Comments on Userinfo equals comments.CommentingUser into userComments
                        join post in _context.Posts on Userinfo equals post.PostingUser
                        select new Post()
                        {
                            Comments = (ICollection<Comment>)userComments,
                            ImageUrl = post.ImageUrl,
                            Content = post.Content,
                            DateOfPost = post.DateOfPost,
                            PostId = post.PostId,
                            PostingUser = new UserInfo() { Comments = Userinfo.Comments, Name = Userinfo.Name, NumberOfComments = Userinfo.NumberOfComments, NumberOfPosts = Userinfo.NumberOfPosts, Posts = Userinfo.Posts, ProfilPictureUrl = Userinfo.ProfilPictureUrl, RegisterDate = Userinfo.RegisterDate, UserInfoID = Userinfo.UserInfoID, Username = Userinfo.Username },
                            Title = post.Title
                        };
            PostDTO dto = AutoMapper.Mapper.Map<Post, PostDTO>(posts.FirstOrDefault(c => c.PostId == id));
            if (dto is null)
            {
                return NotFound();
            }

            return Ok(dto);
        }
        [HttpGet("{id}/comments")]
        public IActionResult GetComments([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var comment = _context.Comments;

            var posts = from Userinfo in _context.UserInfos
                        join comments in _context.Comments on Userinfo equals comments.CommentingUser
                        join post in _context.Posts on Userinfo equals post.PostingUser
                        select new Comment()
                        {
                            Content = comments.Content,
                            CommentId = comments.CommentId,
                            CommentingUser = comments.CommentingUser,
                            DateOfComment = comments.DateOfComment,
                            Post = comments.Post
                        };
            var gettingPosts = AutoMapper.Mapper.Map<IEnumerable<Comment>, IEnumerable<CommentDTO>>(posts);
            if (gettingPosts is null)
            {
                return NotFound();
            }
            return Ok(gettingPosts.Where(c => c.Post.PostId == id));
        }

        // PUT: api/Posts/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPost([FromRoute] int id, [FromBody] PostDTO post)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != post.PostId)
            {
                return BadRequest();
            }
            if(post.PostingUserID > 0)
            {
                post.PostingUser = AutoMapper.Mapper.Map<UserInfo,UserinfoDTO>(_context.UserInfos.FirstOrDefault(u => u.UserInfoID == post.PostingUserID));
            }
            Post editedPost = AutoMapper.Mapper.Map<PostDTO, Post>(post);
            _context.Entry(editedPost).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PostExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Posts
        [HttpPost]
        public async Task<IActionResult> PostPost([FromBody] PostDTO post)
        {
            Post savingPost = null;
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            else if(post.PostingUserID is 0)
            {
                return BadRequest(ModelState);
            }
            else
            {
                savingPost = AutoMapper.Mapper.Map<PostDTO, Post>(post);
                savingPost.PostingUser = _context.UserInfos.FirstOrDefault(c => c.UserInfoID == savingPost.PostingUser.UserInfoID);
                _context.Posts.Add(savingPost);
                await _context.SaveChangesAsync();
            }
            post = AutoMapper.Mapper.Map<Post, PostDTO>(savingPost);
            return CreatedAtAction("GetPost", new { id = savingPost.PostId }, post);
        }

        // DELETE: api/Posts/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Ok(post);
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.PostId == id);
        }
    }
}
