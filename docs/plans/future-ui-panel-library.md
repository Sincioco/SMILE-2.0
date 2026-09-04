# Future SMILE UI and Panel Library

SMILE 2.0 should gain a reusable, beginner-friendly UI and panel library. The Character 3D Viewer and Animation Editor is the immediate design reference because it currently implements panels, labels, buttons, sliders, toggle state, pointer blocking, pointer capture, and timeline controls directly in application source.

The future library should keep these controls in SMILE source where practical, work consistently on native Windows and Web, preserve logical-canvas input semantics, and avoid coupling UI controls to cameras or game actions. Slider and other drag controls must capture their initiating pointer until release so application content cannot react to the same drag.

This is a long-term direction, not part of the current viewer milestone.
