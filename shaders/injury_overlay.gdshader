shader_type canvas_item;

uniform float damage_amount : hint_range(0.0, 1.0) = 0.0;
uniform vec4 vignette_color : source_color = vec4(0.6, 0.0, 0.0, 1.0);

void fragment() {
    vec4 screen = texture(TEXTURE, UV);

    // --- Vignette (red edges) ---
    // Distance from center, 0 at center, 1 at corners
    vec2 uv_centered = UV * 2.0 - 1.0;
    float dist = length(uv_centered);
    float vignette = smoothstep(0.2, 1.2, dist * (0.5 + damage_amount));
    
    // --- Desaturation ---
    float luminance = dot(screen.rgb, vec3(0.299, 0.587, 0.114));
    vec3 desaturated = mix(screen.rgb, vec3(luminance), damage_amount * 0.85);

    // --- Darkening ---
    vec3 darkened = desaturated * (1.0 - damage_amount * 0.5);

    // --- Blend red vignette over the darkened scene ---
    vec3 final_color = mix(darkened, vignette_color.rgb, vignette * damage_amount);

    COLOR = vec4(final_color, damage_amount * 0.6);
}