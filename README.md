# Fluff ML
e621 machine learning based on your likes and dislikes!

## How does this work
The application will take you through the process of building and using your model. 

This isn't perfect, and will depend highly on your likes and dislikes, it may not work as well for you as it does for others.

Here's a step by step on what you'll do, why, and some things to consider before starting. 

1. Login to e621
	- In order to get your likes, your username and API key are required.
	- You can choose to keep your login info ready each time you open the app, or to forget it on app close. 
2. Load posts from your likes and dislikes, and save them to disk
	- Your model will be trained to classify images into two labels, liked or disliked, and the model will guess which one any image is. 
	- This requires we save images to the disk, yes even the ones you disliked. Keep that in mind. 
	- Images are saved as sample quality, cropped into squares. 
	- This works best when you have lots of likes and dislikes, and when you have around the same number of both. So if you aren't happy with your model, try to like and dislike some more images.
	- Depending on how many likes / dislikes you have and your connection speed, this can take awhile. 
	- If you wanna keep updating your model, keep these images on your disk! The post loader skips over images you already have, so you can more quickly update your dataset. 
3. Build your model
	- You'll now get to train your model and you know what that means, more waiting!
	- This step can take as long as you set, but longer is usually better, especially with larger datasets. 
	- When done, the model is reusable. So next time you open the app you can easily bypass the first two steps. 
4. Predict
	- Your model is now ready for use, so let's use it! Fluff ML will download posts from e621 and predict if you'd like to see them or not! 
	- You can choose what tag to search, and how many posts to get.
	- You'll see all of them, with the label guessed by your model. 
	- You can easily like / dislike these posts from the app to update your dataset. You can reload and repredict any time, this will delete the last predicted images on disk. 

Sound like fun? Then give the app a try!
