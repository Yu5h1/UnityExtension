This package contains third-party software components governed by the license(s) indicated below:
---------

Component Name: unified-solver

Source: https://github.com/JohannHotzel/unified-solver

Location in this package: `Runtime/Dependencies/unified-solver` and `Editor/Dependencies/unified-solver` (vendored copy).

Modifications: `Runtime/Core/SolverManager.cs` is modified locally (manager claims the active instance, re-registers enabled colliders, clears the instance cache only when it is the current instance). All other vendored files are unchanged.

The rest of this package (the Yu5h1Lib extension layer) is original work that extends unified-solver.

License Type: MIT

MIT License

Copyright (c) 2026 Johann

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
