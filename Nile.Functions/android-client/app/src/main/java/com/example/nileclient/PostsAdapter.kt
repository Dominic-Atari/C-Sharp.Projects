package com.example.nileclient

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.example.nileclient.databinding.ListItemPostBinding

class PostsAdapter : ListAdapter<PostResponse, PostsAdapter.ViewHolder>(Diff) {

    object Diff : DiffUtil.ItemCallback<PostResponse>() {
        override fun areItemsTheSame(oldItem: PostResponse, newItem: PostResponse): Boolean =
            oldItem.postId == newItem.postId

        override fun areContentsTheSame(oldItem: PostResponse, newItem: PostResponse): Boolean =
            oldItem == newItem
    }

    inner class ViewHolder(private val binding: ListItemPostBinding) : RecyclerView.ViewHolder(binding.root) {
        fun bind(item: PostResponse) {
            binding.postContent.text = item.content ?: "(no content)"
            binding.postMeta.text = "User: ${item.userId} • ${item.createdAt}"
        }
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val inflater = LayoutInflater.from(parent.context)
        val binding = ListItemPostBinding.inflate(inflater, parent, false)
        return ViewHolder(binding)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        holder.bind(getItem(position))
    }
}
