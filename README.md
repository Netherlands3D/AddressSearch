> Archived – Now maintained in the Netherlands3D Main Repository; OpenUPM updates suspended.

 
Address Search
==============

This package provides an auto-suggest Dropdown UI element. With this, a building associated with a Dutch address 
can be found, and its BAG id returned using an event.

When the option to move the camera is enabled, it will also move the Camera to a location matching the Coordinates 
for the given object and tilt the Camera towards it.

Usage
-----

Create a Canvas and InputSystem, and drop the SearchPanel prefab on. After that you can enter Play Mode and try it out 
by typing 2 characters or more.

The SearchPanel is restricted to search for addresses confined to a single city -by default: `Amsterdam`-. To change 
this, see the next chapter on customizing.

When an address is selected, the `onSelectedBuildings` event is fired (see the section on events) and the presented list 
of BAG identifiers can be used in your own code. Do note, by default the Camera will also move to that location; this 
can be disabled using the `Move Camera` option.

Customizing
-----------

The Search Panel contains a GameObject "SearchInput", whose Address Search component has all customizable properties, here are the most important ones and their effect:

Search within City (string)
: Restrict the search to the given name of a city.

Characters needed (int)
: Start searching after the given number of characters

Move Camera (bool)
: Whether to move the Camera to the position of the found address / object and tilt the camera down.

Events
------

onClearButtonToggled (BoolEvent)
: Whether to show or hide the Clear button.

onSelectedBuildings (String List Event)
: When the user selects a suggestion, this event is fired with a list of strings representing the BAG identifiers 
associated with that address.

# Repository Archived

This repository has been **archived** and is no longer maintained independently.

The contents of this package have been merged into the  
**[Netherlands3D Main Repository](https://github.com/Netherlands3D/twin)**  
into the `Packages` folder.

## Current Location

The latest version and future updates of this package will be maintained inside the Netherlands3D main repository as part of an effort to simplify and unify our development workflow.

## Why was this repository archived?

To streamline development and reduce overhead, the Netherlands3D packages are being integrated into the main repository.  
This approach allows for better coordination between packages and features, while we continue to evaluate whether a full monorepo setup (including versioning) is desirable in the future.

## Where to contribute

Please open issues and pull requests in the [Netherlands3D Main Repository](https://github.com/Netherlands3D/twin).

## Publication status

Updates to this package on **OpenUPM** are **suspended until further notice**.  
Future releases will be managed from the main repository once the new development flow has been finalized.

## Historical reference

This repository remains available in read-only mode as a historical record of its standalone development.
