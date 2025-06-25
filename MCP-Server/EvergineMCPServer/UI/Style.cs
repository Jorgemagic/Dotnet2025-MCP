using Evergine.Bindings.Imgui;
using Evergine.Mathematics;

namespace EvergineMCPServer.UI
{
    internal static class Style
    {
        public static unsafe void Apply()
        {
            ImGuiStyle* style = ImguiNative.igGetStyle();
            ImguiNative.igStyleColorsDark(style);
            style->Alpha = 1.0f;
            style->FrameRounding = 3.0f;
            style->ChildBorderSize = 1.0f;
            style->FrameBorderSize = 0.0f;
            style->PopupBorderSize = 1.0f;
            style->WindowBorderSize = 0.0f;
            style->WindowRounding = 8.0f;    // Softer rounded corners for windows
            style->FrameRounding = 4.0f;     // Rounded corners for frames
            style->ScrollbarRounding = 6.0f; // Rounded corners for scrollbars
            style->GrabRounding = 4.0f;      // Rounded corners for grab elements
            style->ChildRounding = 4.0f;     // Rounded corners for child windows

            style->WindowTitleAlign = new Vector2(0.50f, 0.50f); // Centered window title
            style->WindowPadding = new Vector2(10.0f, 10.0f);    // Comfortable padding
            style->FramePadding = new Vector2(6.0f, 4.0f);       // Frame padding
            style->ItemSpacing = new Vector2(8.0f, 8.0f);        // Item spacing
            style->ItemInnerSpacing = new Vector2(8.0f, 6.0f);   // Inner item spacing
            style->IndentSpacing = 22.0f;                   // Indentation spacing

            style->ScrollbarSize = 16.0f; // Scrollbar size
            style->GrabMinSize = 10.0f;   // Minimum grab size

            style->AntiAliasedLines = 1; // Enable anti-aliased lines
            style->AntiAliasedFill = 1;  // Enable anti-aliased fill

            style->Colors_0 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);  // White text for readability
            style->Colors_1 = new Vector4(0.27f, 0.32f, 0.45f, 1.0f); // Subtle grey for disabled text
            style->Colors_2 = new Vector4(0.08f, 0.09f, 0.10f, 1.0f); // Dark background with a hint of blue
            style->Colors_3 = new Vector4(0.09f, 0.10f, 0.12f, 1.0f); // Slightly lighter for child elements
            style->Colors_4 = new Vector4(0.08f, 0.09f, 0.10f, 1.0f); // Popup background
            style->Colors_5 = new Vector4(0.16f, 0.17f, 0.19f, 1.0f); // Soft border color
            style->Colors_6 = new Vector4(0.08f, 0.09f, 0.10f, 1.0f); // No border shadow
            style->Colors_7 = new Vector4(0.11f, 0.13f, 0.15f, 1.0f); // Frame background
            style->Colors_8 = new Vector4(0.16f, 0.17f, 0.19f, 1.0f); // Frame hover effect
            style->Colors_9 = new Vector4(0.16f, 0.17f, 0.19f, 1.0f); // Active frame background
            style->Colors_10 = new Vector4(0.05f, 0.05f, 0.07f, 1.0f); // Title background
            style->Colors_11 = new Vector4(0.05f, 0.05f, 0.07f, 1.0f); // Active title background
            style->Colors_12 = new Vector4(0.08f, 0.09f, 0.10f, 1.0f); // Collapsed title background
            style->Colors_13 = new Vector4(0.10f, 0.11f, 0.12f, 1.0f); // Menu bar background
            style->Colors_14 = new Vector4(0.05f, 0.05f, 0.07f, 1.0f); // Scrollbar background
            style->Colors_15 = new Vector4(0.12f, 0.13f, 0.15f, 1.0f); // Dark accent for scrollbar grab
            style->Colors_16 = new Vector4(0.16f, 0.17f, 0.19f, 1.0f); // Scrollbar grab hover
            style->Colors_17 = new Vector4(0.12f, 0.13f, 0.15f, 1.0f); // Scrollbar grab active
            style->Colors_18 = new Vector4(0.0f, 0.4f, 0.7f, 1.0f);     // Checkmark
            style->Colors_19 = new Vector4(0.0f, 0.4f, 0.7f, 1.0f);     // Slider grab
            style->Colors_20 = new Vector4(0.29f, 0.58f, 0.81f, 1.0f); // Active slider grab
            style->Colors_21 = new Vector4(0.12f, 0.13f, 0.15f, 1.0f); // Dark button
            style->Colors_22 = new Vector4(0.18f, 0.19f, 0.20f, 1.0f); // Button hover effect
            style->Colors_23 = new Vector4(0.15f, 0.15f, 0.15f, 1.0f); // Active button
            style->Colors_24 = new Vector4(0.14f, 0.16f, 0.21f, 1.0f); // Header color similar to button
            style->Colors_25 = new Vector4(0.11f, 0.11f, 0.11f, 1.0f); // Header hover effect
            style->Colors_26 = new Vector4(0.08f, 0.09f, 0.10f, 1.0f); // Active header
            style->Colors_27 = new Vector4(0.13f, 0.15f, 0.19f, 1.0f); // Separator color
            style->Colors_28 = new Vector4(0.16f, 0.18f, 0.25f, 1.0f); // Hover effect for separator
            style->Colors_29 = new Vector4(0.16f, 0.18f, 0.25f, 1.0f); // Active separator
            style->Colors_30 = new Vector4(0.15f, 0.15f, 0.15f, 1.0f); // Resize grip
            style->Colors_31 = new Vector4(0.97f, 1.0f, 0.50f, 1.0f); // Active resize grip
            style->Colors_32 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // Active resize grip
            style->Colors_33 = new Vector4(0.08f, 0.09f, 0.10f, 1.0f); // Inactive tab
            style->Colors_34 = new Vector4(0.12f, 0.13f, 0.15f, 1.0f); // Hover effect for tab
            style->Colors_35 = new Vector4(0.12f, 0.13f, 0.15f, 1.0f); // Active tab color
            style->Colors_36 = new Vector4(0.08f, 0.09f, 0.10f, 1.0f); // Unfocused tab
            style->Colors_37 = new Vector4(0.13f, 0.27f, 0.57f, 1.0f); // Active but unfocused tab
            style->Colors_40 = new Vector4(0.52f, 0.60f, 0.70f, 1.0f); // Plot lines
            style->Colors_41 = new Vector4(0.00f, 0.56f, 1.0f, 1.0f);   // Hover effect for plot lines
            style->Colors_42 = new Vector4(0.0f, 0.39f, 0.69f, 1.0f); // Histogram color
            style->Colors_43 = new Vector4(0.0f, 0.56f, 1.0f, 1.0f); // Hover effect for histogram
            style->Colors_44 = new Vector4(0.05f, 0.05f, 0.07f, 1.0f); // Table header background
            style->Colors_45 = new Vector4(0.05f, 0.05f, 0.07f, 1.0f); // Strong border for tables
            style->Colors_46 = new Vector4(0.0f, 0.0f, 0.0f, 1.0f); // No border for tables
            style->Colors_47 = new Vector4(0.12f, 0.13f, 0.15f, 1.0f); // Table row background
            style->Colors_48 = new Vector4(0.10f, 0.11f, 0.12f, 1.0f); // Alternate row background
            style->Colors_49 = new Vector4(0.54f, 0.54f, 0.54f, 1.0f); // Selected text background
            style->Colors_50 = new Vector4(0.50f, 0.51f, 1.0f, 1.0f); // Drag and drop target
            style->Colors_51 = new Vector4(0.27f, 0.29f, 1.0f, 1.0f); // Navigation highlight
            style->Colors_52 = new Vector4(0.50f, 0.51f, 1.0f, 1.0f); // Windowing highlight
            style->Colors_53 = new Vector4(0.20f, 0.18f, 0.55f, 0.50f); // Dim background for windowing
            style->Colors_54 = new Vector4(0.22f, 0.22f, 0.25f, 0.50f); // Dim background for modal windows
        }
    }
}
