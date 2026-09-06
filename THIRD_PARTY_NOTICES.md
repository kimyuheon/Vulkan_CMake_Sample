# Third-Party Notices — VulkanCAD SDK 및 샘플

이 저장소와 `sdk/` 배포본(공유 라이브러리, iOS 정적 라이브러리, 런타임 에셋)에는 아래의
서드파티 소프트웨어와 에셋이 포함되어 있습니다. 각 항목의 저작권 고지와 라이선스 본문을
이 문서에 수록합니다. 라이선스 원문은 문서 뒷부분의 "라이선스 본문" 절에 있습니다.

> **배포 시 해야 할 일**
> - 이 파일을 배포물(레포, Releases 압축 파일, 앱 번들)에 그대로 포함하세요.
> - 앱으로 배포한다면 설정/정보 화면에 같은 내용을 볼 수 있는 항목을 두세요.
> - `sdk/models/` 의 일부 모델은 배포 전 제거를 권장합니다. 아래 "런타임 에셋" 절 참조.
> - 이 문서는 서드파티 고지이며, VulkanCAD 자체의 라이선스는 별도 `LICENSE` 파일로 정합니다.

---

## 1. 라이브러리 (libVulkanCADCore, libVulkanCADCoreStatic 에 링크됨)

| 구성요소 | 저작권 | 라이선스 | 비고 |
|---|---|---|---|
| Vulkan Loader (`libvulkan.1.dylib`) | Copyright (c) 2014-2025 The Khronos Group Inc., Valve Corporation, LunarG, Inc. | Apache-2.0 | macOS 배포본에 동봉 |
| MoltenVK | Copyright (c) 2015-2025 The Brenwill Workshop Ltd. | Apache-2.0 | iOS 정적 라이브러리에 정적 링크 |
| Manifold | Copyright 2020-2024 The Manifold Authors | Apache-2.0 | 불리언(CSG) 연산 |
| Clipper2 | Copyright (c) 2010-2024 Angus Johnson | BSL-1.0 | Manifold 의존성 |
| Dear ImGui | Copyright (c) 2014-2026 Omar Cornut | MIT | |
| GLFW | Copyright (c) 2002-2006 Marcus Geelnard, Copyright (c) 2006-2019 Camilla Löwy | zlib/libpng | 데스크톱 창 관리 |
| stb (`stb_image`, `stb_image_write`, `stb_truetype`) | Copyright (c) 2017 Sean Barrett | MIT | |
| tinyobjloader | Copyright (c) 2012-Present, Syoyo Fujita and many contributors | MIT | |
| tinygltf | Copyright (c) 2015-Present Syoyo Fujita, Aurélien Chatelain and many contributors | MIT | |
| nlohmann/json | Copyright (c) 2013-2019 Niels Lohmann | MIT | tinygltf 에 동봉 |
| GLM | Copyright (c) 2005 G-Truc Creation | MIT | 헤더 온리 |
| Vulkan Memory Allocator (VMA) | Copyright (c) 2017-2025 Advanced Micro Devices, Inc. All rights reserved. | MIT | 헤더 온리 |
| earcut.hpp | Copyright (c) 2015 Mapbox | ISC | 다각형 삼각분할 |

## 2. 런타임 에셋 (`sdk/fonts`, `sdk/models`, `sdk/textures`)

### 폰트

| 파일 | 저작권 | 라이선스 | 의무 |
|---|---|---|---|
| `NotoSansKR-Regular.ttf` | (c) 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source'. | SIL OFL 1.1 | OFL 본문 동봉 필수. 폰트 단독 판매 금지. 수정 시 'Source' 이름 사용 금지 |
| `Roboto-Medium.ttf` | Copyright 2011 Google Inc. All Rights Reserved. Roboto is a trademark of Google. | Apache-2.0 | 고지 + 본문 동봉 |

### 3D 모델

glTF 모델은 Khronos Group 의 glTF-Sample-Assets / glTF-Sample-Models 저장소에서 가져왔습니다.
제작자 표기는 해당 저장소의 `Models/Models.md` 를 기준으로 했습니다.

| 파일 | 제작자 / 저작권 | 라이선스 | 비고 |
|---|---|---|---|
| `BoomBox.glb` | Microsoft, (c) 2017 | CC0-1.0 | 의무 없음 |
| `Box With Spaces.gltf/.bin` | Ed Mackey / Analytical Graphics, Inc., (c) 2017 | CC0-1.0 | 의무 없음. glTF 로고는 Khronos 상표 |
| `BoxAnimated.glb` | Cesium, (c) 2017 | CC-BY-4.0 | 제작자 표기 필수 |
| `CesiumMilkTruck.glb` | Cesium, (c) 2017 | CC-BY-4.0 | 제작자 표기 필수. Cesium 로고는 Cesium 상표 |
| `Duck.gltf` | Sony, (c) 2006 | SCEA Shared Source License 1.0 | 본문 동봉 필수 |
| `Fox.glb` | 모델: PixelMannen (CC0, 2014). 리깅/애니메이션: tomkranis (CC-BY-4.0, 2014). glTF 변환: @AsoboStudio, @scurest (CC-BY-4.0, 2017) | CC-BY-4.0 | 제작자 표기 필수 |
| `InterpolationTest.glb` | Khronos, (c) 2017 | CC0-1.0 | 의무 없음 |
| `SheenChair.glb` | Eric Chadwick / Wayfair, LLC, (c) 2020 | CC0-1.0 | 의무 없음 |
| `2CylinderEngine.gltf/.glb` | Okino Computer Graphics 가 JT 에서 변환. 원저작자 및 라이선스 미명시 | **불명** | **배포 전 제거 권장** |
| `GearboxAssy.glb` | Okino Computer Graphics 가 JT 에서 변환. 원저작자 및 라이선스 미명시 | **불명** | **배포 전 제거 권장** |
| `cube.obj`, `colored_cube.obj`, `quad.obj`, `flat_vase.obj`, `smooth_vase.obj` | Copyright (c) 2020 Brendan Galea (littleVulkanEngine 튜토리얼) | MIT | 저작권 고지 |
| `demo_arm.urdf` | 자체 제작 | (VulkanCAD 라이선스 따름) | |

> 2CylinderEngine 과 GearboxAssy 는 Khronos 가 새 저장소(glTF-Sample-Assets)로 옮기면서
> 라이선스가 확인되지 않아 **제외한 모델**입니다. 재배포 근거가 없으므로 배포본에서 빼는 것을 권장합니다.

### 텍스처

| 파일 | 출처 | 라이선스 |
|---|---|---|
| `Bricks104_4K-PNG_Color.png` | ambientCG (https://ambientcg.com) | CC0-1.0 |
| `Tiles_Stone_005_basecolor.png` | 3dtextures.me (https://3dtextures.me) | CC0-1.0 |

## 3. 샘플이 사용하는 프레임워크 (SDK 에는 미포함)

| 샘플 | 프레임워크 | 라이선스 | 비고 |
|---|---|---|---|
| `qml_test` | Qt 6 | LGPL-3.0 | 소스 배포는 무관. **빌드된 실행 파일을 배포하면** 동적 링크, LGPL 본문 동봉, 재링크 가능성 보장 필요 |
| `mfc_test`, `mfc_dlg_test`, `wpf_test` | MFC / .NET | Microsoft 재배포 조건 | Visual Studio / .NET 런타임 재배포 약관 따름 |
| `android_test` | Android SDK/NDK | Apache-2.0 등 | Android SDK 약관 따름 |

## 4. 상표

- **Vulkan** 및 Vulkan 로고는 The Khronos Group Inc. 의 등록 상표입니다.
- **Metal**, **Xcode** 는 Apple Inc. 의 상표입니다.
- **Roboto** 는 Google 의 상표, **Source** 는 Adobe 의 상표입니다.
- **Cesium** 로고는 Cesium 의 상표입니다.

---

## 라이선스 본문

### Apache License 2.0

적용: Vulkan Loader, MoltenVK, Manifold, Roboto 폰트

```
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION

   1. Definitions.

      "License" shall mean the terms and conditions for use, reproduction,
      and distribution as defined by Sections 1 through 9 of this document.

      "Licensor" shall mean the copyright owner or entity authorized by
      the copyright owner that is granting the License.

      "Legal Entity" shall mean the union of the acting entity and all
      other entities that control, are controlled by, or are under common
      control with that entity. For the purposes of this definition,
      "control" means (i) the power, direct or indirect, to cause the
      direction or management of such entity, whether by contract or
      otherwise, or (ii) ownership of fifty percent (50%) or more of the
      outstanding shares, or (iii) beneficial ownership of such entity.

      "You" (or "Your") shall mean an individual or Legal Entity
      exercising permissions granted by this License.

      "Source" form shall mean the preferred form for making modifications,
      including but not limited to software source code, documentation
      source, and configuration files.

      "Object" form shall mean any form resulting from mechanical
      transformation or translation of a Source form, including but
      not limited to compiled object code, generated documentation,
      and conversions to other media types.

      "Work" shall mean the work of authorship, whether in Source or
      Object form, made available under the License, as indicated by a
      copyright notice that is included in or attached to the work
      (an example is provided in the Appendix below).

      "Derivative Works" shall mean any work, whether in Source or Object
      form, that is based on (or derived from) the Work and for which the
      editorial revisions, annotations, elaborations, or other modifications
      represent, as a whole, an original work of authorship. For the purposes
      of this License, Derivative Works shall not include works that remain
      separable from, or merely link (or bind by name) to the interfaces of,
      the Work and Derivative Works thereof.

      "Contribution" shall mean any work of authorship, including
      the original version of the Work and any modifications or additions
      to that Work or Derivative Works thereof, that is intentionally
      submitted to Licensor for inclusion in the Work by the copyright owner
      or by an individual or Legal Entity authorized to submit on behalf of
      the copyright owner. For the purposes of this definition, "submitted"
      means any form of electronic, verbal, or written communication sent
      to the Licensor or its representatives, including but not limited to
      communication on electronic mailing lists, source code control systems,
      and issue tracking systems that are managed by, or on behalf of, the
      Licensor for the purpose of discussing and improving the Work, but
      excluding communication that is conspicuously marked or otherwise
      designated in writing by the copyright owner as "Not a Contribution."

      "Contributor" shall mean Licensor and any individual or Legal Entity
      on behalf of whom a Contribution has been received by Licensor and
      subsequently incorporated within the Work.

   2. Grant of Copyright License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      copyright license to reproduce, prepare Derivative Works of,
      publicly display, publicly perform, sublicense, and distribute the
      Work and such Derivative Works in Source or Object form.

   3. Grant of Patent License. Subject to the terms and conditions of
      this License, each Contributor hereby grants to You a perpetual,
      worldwide, non-exclusive, no-charge, royalty-free, irrevocable
      (except as stated in this section) patent license to make, have made,
      use, offer to sell, sell, import, and otherwise transfer the Work,
      where such license applies only to those patent claims licensable
      by such Contributor that are necessarily infringed by their
      Contribution(s) alone or by combination of their Contribution(s)
      with the Work to which such Contribution(s) was submitted. If You
      institute patent litigation against any entity (including a
      cross-claim or counterclaim in a lawsuit) alleging that the Work
      or a Contribution incorporated within the Work constitutes direct
      or contributory patent infringement, then any patent licenses
      granted to You under this License for that Work shall terminate
      as of the date such litigation is filed.

   4. Redistribution. You may reproduce and distribute copies of the
      Work or Derivative Works thereof in any medium, with or without
      modifications, and in Source or Object form, provided that You
      meet the following conditions:

      (a) You must give any other recipients of the Work or
          Derivative Works a copy of this License; and

      (b) You must cause any modified files to carry prominent notices
          stating that You changed the files; and

      (c) You must retain, in the Source form of any Derivative Works
          that You distribute, all copyright, patent, trademark, and
          attribution notices from the Source form of the Work,
          excluding those notices that do not pertain to any part of
          the Derivative Works; and

      (d) If the Work includes a "NOTICE" text file as part of its
          distribution, then any Derivative Works that You distribute must
          include a readable copy of the attribution notices contained
          within such NOTICE file, excluding those notices that do not
          pertain to any part of the Derivative Works, in at least one
          of the following places: within a NOTICE text file distributed
          as part of the Derivative Works; within the Source form or
          documentation, if provided along with the Derivative Works; or,
          within a display generated by the Derivative Works, if and
          wherever such third-party notices normally appear. The contents
          of the NOTICE file are for informational purposes only and
          do not modify the License. You may add Your own attribution
          notices within Derivative Works that You distribute, alongside
          or as an addendum to the NOTICE text from the Work, provided
          that such additional attribution notices cannot be construed
          as modifying the License.

      You may add Your own copyright statement to Your modifications and
      may provide additional or different license terms and conditions
      for use, reproduction, or distribution of Your modifications, or
      for any such Derivative Works as a whole, provided Your use,
      reproduction, and distribution of the Work otherwise complies with
      the conditions stated in this License.

   5. Submission of Contributions. Unless You explicitly state otherwise,
      any Contribution intentionally submitted for inclusion in the Work
      by You to the Licensor shall be under the terms and conditions of
      this License, without any additional terms or conditions.
      Notwithstanding the above, nothing herein shall supersede or modify
      the terms of any separate license agreement you may have executed
      with Licensor regarding such Contributions.

   6. Trademarks. This License does not grant permission to use the trade
      names, trademarks, service marks, or product names of the Licensor,
      except as required for reasonable and customary use in describing the
      origin of the Work and reproducing the content of the NOTICE file.

   7. Disclaimer of Warranty. Unless required by applicable law or
      agreed to in writing, Licensor provides the Work (and each
      Contributor provides its Contributions) on an "AS IS" BASIS,
      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
      implied, including, without limitation, any warranties or conditions
      of TITLE, NON-INFRINGEMENT, MERCHANTABILITY, or FITNESS FOR A
      PARTICULAR PURPOSE. You are solely responsible for determining the
      appropriateness of using or redistributing the Work and assume any
      risks associated with Your exercise of permissions under this License.

   8. Limitation of Liability. In no event and under no legal theory,
      whether in tort (including negligence), contract, or otherwise,
      unless required by applicable law (such as deliberate and grossly
      negligent acts) or agreed to in writing, shall any Contributor be
      liable to You for damages, including any direct, indirect, special,
      incidental, or consequential damages of any character arising as a
      result of this License or out of the use or inability to use the
      Work (including but not limited to damages for loss of goodwill,
      work stoppage, computer failure or malfunction, or any and all
      other commercial damages or losses), even if such Contributor
      has been advised of the possibility of such damages.

   9. Accepting Warranty or Additional Liability. While redistributing
      the Work or Derivative Works thereof, You may choose to offer,
      and charge a fee for, acceptance of support, warranty, indemnity,
      or other liability obligations and/or rights consistent with this
      License. However, in accepting such obligations, You may act only
      on Your own behalf and on Your sole responsibility, not on behalf
      of any other Contributor, and only if You agree to indemnify,
      defend, and hold each Contributor harmless for any liability
      incurred by, or claims asserted against, such Contributor by reason
      of your accepting any such warranty or additional liability.

   END OF TERMS AND CONDITIONS

   APPENDIX: How to apply the Apache License to your work.

      To apply the Apache License to your work, attach the following
      boilerplate notice, with the fields enclosed by brackets "[]"
      replaced with your own identifying information. (Don't include
      the brackets!)  The text should be enclosed in the appropriate
      comment syntax for the file format. We also recommend that a
      file or class name and description of purpose be included on the
      same "printed page" as the copyright notice for easier
      identification within third-party archives.

   Copyright [yyyy] [name of copyright owner]

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
```

### MIT License

적용: Dear ImGui, stb, tinyobjloader, tinygltf, nlohmann/json, GLM, Vulkan Memory Allocator,
littleVulkanEngine 샘플 모델(OBJ)

각 구성요소의 저작권 표시는 위 표를 따르며, 본문은 아래와 같습니다.

```
Copyright (c) 2014-2026 Omar Cornut                                   (Dear ImGui)
Copyright (c) 2017 Sean Barrett                                       (stb)
Copyright (c) 2012-Present, Syoyo Fujita and many contributors        (tinyobjloader)
Copyright (c) 2015-Present Syoyo Fujita, Aurélien Chatelain and many contributors (tinygltf)
Copyright (c) 2013-2019 Niels Lohmann <http://nlohmann.me>            (nlohmann/json)
Copyright (c) 2005 G-Truc Creation                                    (GLM)
Copyright (c) 2017-2025 Advanced Micro Devices, Inc. All rights reserved. (VMA)
Copyright (c) 2020 Brendan Galea                                      (littleVulkanEngine)

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
```

### zlib/libpng License

적용: GLFW

```
Copyright (c) 2002-2006 Marcus Geelnard
Copyright (c) 2006-2019 Camilla Löwy

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would
   be appreciated but is not required.

2. Altered source versions must be plainly marked as such, and must not
   be misrepresented as being the original software.

3. This notice may not be removed or altered from any source
   distribution.
```

### ISC License

적용: earcut.hpp

```
ISC License

Copyright (c) 2015, Mapbox

Permission to use, copy, modify, and/or distribute this software for any purpose
with or without fee is hereby granted, provided that the above copyright notice
and this permission notice appear in all copies.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
THIS SOFTWARE.
```

### Boost Software License 1.0

적용: Clipper2

```
Boost Software License - Version 1.0 - August 17th, 2003

Permission is hereby granted, free of charge, to any person or organization
obtaining a copy of the software and accompanying documentation covered by
this license (the "Software") to use, reproduce, display, distribute,
execute, and transmit the Software, and to prepare derivative works of the
Software, and to permit third-parties to whom the Software is furnished to
do so, all subject to the following:

The copyright notices in the Software and this entire statement, including
the above license grant, this restriction and the following disclaimer,
must be included in all copies of the Software, in whole or in part, and
all derivative works of the Software, unless such copies or derivative
works are solely in the form of machine-executable object code generated by
a source language processor.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT
SHALL THE COPYRIGHT HOLDERS OR ANYONE DISTRIBUTING THE SOFTWARE BE LIABLE
FOR ANY DAMAGES OR OTHER LIABILITY, WHETHER IN CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
DEALINGS IN THE SOFTWARE.
```

### SIL Open Font License 1.1

적용: Noto Sans KR

```
This Font Software is licensed under the SIL Open Font License,
Version 1.1.

This license is copied below, and is also available with a FAQ at:
http://scripts.sil.org/OFL

-----------------------------------------------------------
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
-----------------------------------------------------------

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font
creation efforts of academic and linguistic communities, and to
provide a free and open framework in which fonts may be shared and
improved in partnership with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded,
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply to
any document created using the fonts or their derivatives.

DEFINITIONS
"Font Software" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

"Reserved Font Name" refers to any names specified as such after the
copyright statement(s).

"Original Version" refers to the collection of Font Software
components as distributed by the Copyright Holder(s).

"Modified Version" refers to any derivative made by adding to,
deleting, or substituting -- in part or in whole -- any of the
components of the Original Version, by changing formats or by porting
the Font Software to a new environment.

"Author" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed,
modify, redistribute, and sell modified and unmodified copies of the
Font Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components, in
Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3) No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the
corresponding Copyright Holder. This restriction only applies to the
primary font name as presented to the users.

4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5) The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created using
the Font Software.

TERMINATION
This license becomes null and void if any of the above conditions are
not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.
```

### SCEA Shared Source License 1.0

적용: Duck

```
SCEA Shared Source License 1.0

Terms and Conditions:

    1. Definitions:

    "Software" shall mean the software and related documentation, whether in Source or Object Form, made available under this SCEA Shared Source license ("License"), that is indicated by a copyright notice file included in the source files or attached or accompanying the source files.

    "Licensor" shall mean Sony Computer Entertainment America, Inc. (herein "SCEA")

    "Object Code" or "Object Form" shall mean any form that results from translation or transformation of Source Code, including but not limited to compiled object code or conversions to other forms intended for machine execution.
    "Source Code" or "Source Form" shall have the plain meaning generally accepted in the software industry, including but not limited to software source code, documentation source, header and configuration files.

    "You" or "Your" shall mean you as an individual or as a company, or whichever form under which you are exercising rights under this License.
    2. License Grant.

    Licensor hereby grants to You, free of charge subject to the terms and conditions of this License, an irrevocable, non-exclusive, worldwide, perpetual, and royalty-free license to use, modify, reproduce, distribute, publicly perform or display the Software in Object or Source Form .
    3. No Right to File for Patent.
    In exchange for the rights that are granted to You free of charge under this License, You agree that You will not file for any patent application, seek copyright protection or take any other action that might otherwise impair the ownership rights in and to the Software that may belong to SCEA or any of the other contributors/authors of the Software.
    4. Contributions.

    SCEA welcomes contributions in form of modifications, optimizations, tools or documentation designed to improve or expand the performance and scope of the Software (collectively "Contributions"). Per the terms of this License You are free to modify the Software and those modifications would belong to You. You may however wish to donate Your Contributions to SCEA for consideration for inclusion into the Software. For the avoidance of doubt, if You elect to send Your Contributions to SCEA, You are doing so voluntarily and are giving the Contributions to SCEA and its parent company Sony Computer Entertainment, Inc., free of charge, to use, modify or distribute in any form or in any manner. SCEA acknowledges that if You make a donation of Your Contributions to SCEA, such Contributions shall not exclusively belong to SCEA or its parent company and such donation shall not be to Your exclusion. SCEA, in its sole discretion, shall determine whether or not to include Your donated Contributions into the Software, in whole, in part, or as modified by SCEA. Should SCEA elect to include any such Contributions into the Software, it shall do so at its own risk and may elect to give credit or special thanks to any such contributors in the attached copyright notice. However, if any of Your contributions are included into the Software, they will become part of the Software and will be distributed under the terms and conditions of this License. Further, if Your donated Contributions are integrated into the Software then Sony Computer Entertainment, Inc. shall become the copyright owner of the Software now containing Your contributions and SCEA would be the Licensor.
    5. Redistribution in Source Form

    You may redistribute copies of the Software, modifications or derivatives thereof in Source Code Form, provided that You:
        a. Include a copy of this License and any copyright notices with source
        b. Identify modifications if any were made to the Software
        c. Include a copy of all documentation accompanying the Software and modifications made by You
    6. Redistribution in Object Form

    If You redistribute copies of the Software, modifications or derivatives thereof in Object Form only (as incorporated into finished goods, i.e. end user applications) then You will not have a duty to include any copies of the code, this License, copyright notices, other attributions or documentation.
    7. No Warranty

    THE SOFTWARE IS PROVIDED ON AN "AS IS" BASIS, WITHOUT ANY REPRESENTATIONS OR WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING, WITHOUT LIMITATION, ANY WARRANTIES OF TITLE, NON-INFRINGEMENT, MERCHANTABILITY, OR FITNESS FOR A PARTICULAR PURPOSE. YOU ARE SOLELY RESPONSIBLE FOR DETERMINING THE APPROPRIATENESS OF USING, MODIFYING OR REDISTRIBUTING THE SOFTWARE AND ASSUME ANY RISKS ASSOCIATED WITH YOUR EXERCISE OF PERMISSIONS UNDER THIS LICENSE.
    8. Limitation of Liability

    UNDER NO CIRCUMSTANCES AND UNDER NO LEGAL THEORY WILL EITHER PARTY BE LIABLE TO THE OTHER PARTY OR ANY THIRD PARTY FOR ANY DIRECT, INDIRECT, CONSEQUENTIAL, SPECIAL, INCIDENTAL, OR EXEMPLARY DAMAGES WITH RESPECT TO ANY INJURY, LOSS, OR DAMAGE, ARISING UNDER OR IN CONNECTION WITH THIS LETTER AGREEMENT, WHETHER FORESEEABLE OR UNFORESEEABLE, EVEN IF SUCH PARTY HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH INJURY, LOSS, OR DAMAGE. THE LIMITATIONS OF LIABILITY SET FORTH IN THIS SECTION SHALL APPLY TO THE FULLEST EXTENT PERMISSIBLE AT LAW OR ANY GOVERMENTAL REGULATIONS.
    9. Governing Law and Consent to Jurisdiction

    This Agreement shall be governed by and interpreted in accordance with the laws of the State of California, excluding that body of law related to choice of laws, and of the United States of America. Any action or proceeding brought to enforce the terms of this Agreement or to adjudicate any dispute arising hereunder shall be brought in the Superior Court of the County of San Mateo, State of California or the United States District Court for the Northern District of California. Each of the parties hereby submits itself to the exclusive jurisdiction and venue of such courts for purposes of any such action. In addition, each party hereby waives the right to a jury trial in any action or proceeding related to this Agreement.
    10. Copyright Notice for Redistribution of Source Code

Copyright 2005 Sony Computer Entertainment Inc.

Licensed under the SCEA Shared Source License, Version 1.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at:
http://research.scea.com/scea_shared_source_license.html

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
```

### Creative Commons Attribution 4.0 International (CC-BY-4.0)

적용: BoxAnimated (Cesium), CesiumMilkTruck (Cesium), Fox (tomkranis, @AsoboStudio, @scurest)

위 제작자 표기를 유지해야 합니다. 라이선스 전문: https://creativecommons.org/licenses/by/4.0/legalcode

### Creative Commons Zero 1.0 Universal (CC0-1.0)

적용: BoomBox, Box With Spaces, InterpolationTest, SheenChair, Fox 원본 모델(PixelMannen), 텍스처 2종

의무 사항은 없습니다. 전문: https://creativecommons.org/publicdomain/zero/1.0/legalcode
