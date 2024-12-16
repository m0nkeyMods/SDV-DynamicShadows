using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Network;

namespace YourProjectName
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        // TODO is there a variable already avaiable?
        int currentTime = -1;
        bool extended = false;
        bool isOutside = false;
            
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {            
            helper.GameContent.InvalidateCache("LooseSprites/shadow");

            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;
            helper.Events.Content.AssetRequested += this.OnAssetRequest;
            helper.Events.Player.Warped += this.Warped;
        }

        private void OnTimeChange(object sender, TimeChangedEventArgs e)
        {
            currentTime = e.NewTime;
            //this.Monitor.Log($"Time change: {currentTime}.", LogLevel.Info);
            this.Helper.GameContent.InvalidateCache("LooseSprites/shadow");

        }

        private void Warped(object sender, WarpedEventArgs e)
        {
            isOutside = e.NewLocation.IsOutdoors;
            this.Helper.GameContent.InvalidateCache("LooseSprites/shadow");
        }

        private void OnAssetRequest(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("LooseSprites/shadow"))
            {
                e.Edit(asset =>
                {
                    // TODO only do this outside
                    var editor = asset.AsImage();

                    if (!extended)
                    {
                        int tempWidth = editor.Data.Width * 200;
                        int tempHeight = editor.Data.Height * 200;
                        editor.ExtendImage(tempWidth, tempHeight);
                    }

                    IRawTextureData image = this.Helper.ModContent.Load<IRawTextureData>("assets/shadow.png");
                    int currentPixelCount = image.Width * image.Height;

                    // Calculate what the scaling factor should be based on the current time
                    float scalingFactor;
                    float time = (float)currentTime;
                    if (currentTime >= 600 && isOutside)
                    {
                        scalingFactor = 3f * time / 700f - 32f / 7f;
                    } else {
                        scalingFactor = 1;
                    }

                    // Create new array with correct size based on scaled
                    int scaledWidth = (int)Math.Floor(image.Width * scalingFactor);
                    int newWidth = scaledWidth < image.Width && scaledWidth > -image.Width ? image.Width : scaledWidth;

                    int newPixelCount = Math.Abs(newWidth) * image.Height;
                    Color[] newData = new Color[newPixelCount];
                    // bit of a cheeky way to allow for any size image, pulling in a 500x500 image and using what i need
                    IRawTextureData newImage = this.Helper.ModContent.Load<IRawTextureData>("assets/blank.png");

                    // Below is a simple scaling algo
                    for (int i = 0; i < newPixelCount; i++)
                    {
                        // Get the current row
                        float temp = i / Math.Abs(newWidth);
                        int rowNum = (int)Math.Floor(temp);

                        // Get the index in the row of the stretched image
                        int indexInNewRow = i % Math.Abs(newWidth);

                        // Get the aprox index in the old row
                        int indexInOldRow = (image.Width * indexInNewRow) / Math.Abs(newWidth);

                        // Calculate original sequential image index
                        int oldRowStart = rowNum * image.Width;
                        int originalIndex = oldRowStart + indexInOldRow;

                        // Idk if this happens but just in case
                        if (originalIndex > currentPixelCount - 1)
                        {
                            this.Monitor.Log($"Skipping index {i} since its bigger than original image", LogLevel.Info);
                            continue;
                        }

                        Color color = image.Data[originalIndex];
                        if (color.A == 0) continue; // ignore transparent color

                        // Explanation of below: basically newImage is a big transparent image, we are setting the top right chunk
                        //  of that image to the stretched shadow image we want

                     
                        // Get the sequential pixel number for the start of the row in the whole frame
                        int currentRowStart = rowNum * newImage.Width;
                        // Get the sequential index in 
                        int index = currentRowStart + indexInNewRow;

                        newImage.Data[index] = color;
                    }

                    // offset shadow index
                    int offset = (int)Math.Floor(Math.Abs(newWidth) * 0.8);
                    int widthAddition = newWidth > 0 ? offset : newWidth + offset;
                    // Replace shadow with new image
                    Rectangle areaOfOriginal = new Rectangle(editor.Data.Width / 2 - widthAddition, editor.Data.Height / 2 - (image.Height / 2), newImage.Width, newImage.Height);
                    Rectangle areaOfOverwrite = new Rectangle(0, 0, newImage.Width, newImage.Height);
                    editor.PatchImage(newImage, sourceArea: areaOfOverwrite, targetArea: areaOfOriginal);
                });
            }
        }
    }
}