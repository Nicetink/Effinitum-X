using System;

namespace SystemOptimizer.Models
{
    /// <summary>
    /// Defines the available animation presets for system animations
    /// </summary>
    public enum AnimationPreset
    {
        /// <summary>
        /// Unknown or indeterminate state
        /// </summary>
        Unknown = -1,
        
        /// <summary>
        /// All animations disabled (best performance)
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Only minimal window animations enabled
        /// </summary>
        Minimal = 1,
        
        /// <summary>
        /// Basic window and taskbar animations enabled
        /// </summary>
        Basic = 2,
        
        /// <summary>
        /// Standard animations with window effects
        /// </summary>
        Standard = 3,
        
        /// <summary>
        /// Enhanced animations with transparency and more visual effects
        /// </summary>
        Enhanced = 4,
        
        /// <summary>
        /// Maximum animations - all visual effects enabled (best appearance)
        /// </summary>
        Maximum = 5,
        
        /// <summary>
        /// Custom animation settings defined by user
        /// </summary>
        Custom = 6
    }
    
    /// <summary>
    /// Contains information about the current system animation settings
    /// </summary>
    public class AnimationStatus
    {
        /// <summary>
        /// Indicates whether animations are generally enabled
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// The current animation preset being used
        /// </summary>
        public AnimationPreset CurrentPreset { get; set; }
        
        /// <summary>
        /// Window animations enabled (fade, slide)
        /// </summary>
        public bool WindowAnimationsEnabled { get; set; }
        
        /// <summary>
        /// Taskbar animations enabled
        /// </summary>
        public bool TaskbarAnimationsEnabled { get; set; }
        
        /// <summary>
        /// Visual transitions enabled
        /// </summary>
        public bool TransitionsEnabled { get; set; }
        
        /// <summary>
        /// Aero peek (desktop preview) enabled
        /// </summary>
        public bool AeroEnabled { get; set; }
        
        /// <summary>
        /// Window thumbnails enabled
        /// </summary>
        public bool ThumbnailsEnabled { get; set; }
        
        /// <summary>
        /// Window transparency effects enabled
        /// </summary>
        public bool TransparencyEnabled { get; set; }
        
        /// <summary>
        /// Smooth scrolling enabled
        /// </summary>
        public bool SmoothScrollEnabled { get; set; }
        
        /// <summary>
        /// Window and list shadows enabled
        /// </summary>
        public bool ShadowsEnabled { get; set; }
        
        /// <summary>
        /// Converts the current preset to a user-friendly string
        /// </summary>
        public string PresetName => CurrentPreset switch
        {
            AnimationPreset.None => "No Animations",
            AnimationPreset.Minimal => "Minimal Animations",
            AnimationPreset.Basic => "Basic Animations",
            AnimationPreset.Standard => "Standard Animations",
            AnimationPreset.Enhanced => "Enhanced Animations",
            AnimationPreset.Maximum => "Maximum Animations",
            AnimationPreset.Custom => "Custom Settings",
            _ => "Unknown"
        };
    }
} 