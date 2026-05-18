package com.example.nileclient

import retrofit2.http.GET

// DTOs match the backend envelope
// ApiResponse wraps data and error, data is a list for listPosts

data class PostResponse(
    val postId: String,
    val userId: String,
    val content: String?,
    val imageUrl: String?,
    val createdAt: String
)

data class ApiResponse<T>(
    val data: T?,
    val error: Any?
)

interface PostApi {
    // Calls the Functions app under /Functions (route is /api/posts)
    @GET("api/posts?take=50")
    suspend fun listPosts(): ApiResponse<List<PostResponse>>
}
