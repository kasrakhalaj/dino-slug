"""Render the short illustrated HW1 report after validation is complete."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor, white
from reportlab.lib.pagesizes import A4
from reportlab.platypus import Paragraph
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader

ROOT=Path(__file__).resolve().parents[1]
DOCS=ROOT/'Docs'
W,H=A4
M=42
CW=W-2*M
INK=HexColor('#29283f'); GOLD=HexColor('#aa702e'); GRAY=HexColor('#626477')
body=ParagraphStyle('body',fontName='Helvetica',fontSize=10,leading=14,textColor=INK,spaceAfter=9)
small=ParagraphStyle('small',fontName='Helvetica',fontSize=8.5,leading=12,textColor=GRAY)

def paragraph(c,text,y,style=body,width=CW,x=M):
    p=Paragraph(text,style);_,h=p.wrap(width,1000);p.drawOn(c,x,y-h);return y-h-10

def header(c,page,title):
    c.setFillColor(GOLD);c.setFont('Helvetica-Bold',9);c.drawString(M,H-40,'GAME DESIGN / HOMEWORK 1')
    c.setFillColor(INK);c.setFont('Helvetica-Bold',27);c.drawString(M,H-77,title)
    c.setStrokeColor(HexColor('#e6dfd5'));c.line(M,H-92,W-M,H-92)
    c.setFont('Helvetica',8);c.setFillColor(GRAY)
    c.drawString(M,25,'Dino Slug | Unity 6000.5.7f1 | 19 October 2026')
    c.drawRightString(W-M,25,str(page))
    return H-111

def screenshot(c,name,y,width=CW,x=M):
    im=ImageReader(str(DOCS/'Screenshots'/name));iw,ih=im.getSize();h=width*ih/iw
    c.drawImage(im,x,y-h,width,h);return y-h-10

def main():
    output=DOCS/'DinoSlug-HW1-Report.pdf'
    c=canvas.Canvas(str(output),pagesize=A4)
    c.setTitle('Dino Slug - HW1 Implementation Report')
    c.setAuthor('Prepared with assistance from OpenAI Codex')
    y=header(c,1,'DINO / SLUG')
    y=paragraph(c,'A complete 2D dinosaur runner with chunky pixel artwork, a reference-inspired goofy face, layered dusk scenery, and a persistent personal best.',y)
    y=screenshot(c,'01-main-menu.png',y)
    y=paragraph(c,'<b>Controls.</b> Space jumps; either Shift key dashes; Escape pauses or resumes. Menus support mouse input and keyboard navigation. All game and UI input uses the New Input System, with InputSystemUIInputModule on the EventSystem.',y)
    y=paragraph(c,'<b>Core loop.</b> Start with three lives. Jump over cacti, stay grounded beneath pterosaurs, and collect coins. Every obstacle can remove one life once. At zero lives, gameplay ends, the Death animation plays, and the result screen offers restart or return to the menu.',y)
    y=paragraph(c,'<b>Scoring and dash.</b> Score is two points per meter plus 25 per coin. Dash boosts forward travel by 65% for 0.24 seconds with a 2.4-second cooldown. It provides no collision immunity. Normal speed ramps from 7 to a cap of 11 world units per second.',y)
    c.showPage()

    y=header(c,2,'Implementation')
    y=screenshot(c,'02-gameplay.png',y)
    y=paragraph(c,'<b>Object-oriented design.</b> GroundObstacle, AirObstacle, and Coin derive from the abstract Interactable class. It recognizes player contact, consumes each interaction once, and dispatches the subtype behavior. This prevents repeated damage or rewards without type-specific branching in the player.',y)
    y=paragraph(c,'<b>Ownership.</b> RunModel owns lives, score, difficulty, and lifecycle state. GameSession coordinates input, UI, pause, restart, death timing, and persistence. PlayerController owns Rigidbody2D movement and ability timers. ObstacleSpawner spaces the course by traveled distance; ParallaxLayer moves and wraps the scenery.',y)
    y=paragraph(c,'<b>Animation and artwork.</b> The Animator has Run, Jump, Hit, and Death states. The new dot-eye, toothy-grin head is a separate sprite with matching position and rotation curves in all states. Hit tint and effects apply to the head and body. Native pixel sprites use point filtering and a 32-PPU pixel-perfect camera; antialiasing and blur are disabled.',y)
    y=paragraph(c,'<b>Persistence.</b> The HUD updates current score and high score live. PlayerPrefs stores the record under DinoSlug.HighScore.v1. Pause freezes simulation and cooldowns; restart clears run state while preserving the record.',y)
    c.showPage()

    y=header(c,3,'Validation and delivery')
    gap=12;half=(CW-gap)/2
    y1=screenshot(c,'03-pause-menu.png',y,half,M)
    y2=screenshot(c,'04-game-over.png',y,half,M+half+gap)
    y=min(y1,y2)-3
    y=paragraph(c,'Separate Pause and Game Over panels remain interactive while gameplay actions are disabled.',y,small)
    counts=[]
    for file,label in [('editmode-results.xml','EditMode'),('playmode-results.xml','PlayMode')]:
        r=ET.parse(DOCS/file).getroot();counts.append(f'{label}: {r.attrib.get("passed")} passed, {r.attrib.get("failed")} failed')
    y=paragraph(c,'<b>Automated checks.</b> '+'. '.join(counts)+'. Tests exercise three-hit death, scoring, pause, restart, difficulty limits, keyboard jump and dash, physical obstacles and coins, Animator transitions, UI input setup, and score persistence across scene reload.',y)
    evidence_file=DOCS/'Standalone'/'standalone-results.json'
    if evidence_file.exists():
        e=json.loads(evidence_file.read_text())
        if e.get('firstLaunchPassed') and e.get('secondLaunchPassed'):
            y=paragraph(c,'<b>Windows executable.</b> The standalone build passed startup, its Start button, keyboard jump/dash, pause/restart, and Game Over checks. A second process launch loaded the saved record successfully. The check restored the pre-existing score afterward.',y)
    y=paragraph(c,'<b>Files.</b> The Unity scene is Assets/Game/Scenes/DinoSlug.unity. The runnable Windows build is Builds/Windows/DinoSlug.exe with its supporting folder and DLLs. Source code, prefabs, animation clips, input actions, original body-art generator, generated head source, tests, and documentation are included in the local Git project.',y)
    y=paragraph(c,'<b>Submission details still required.</b> Publish the local repository to the chosen Git host and include its accessible URL. Fill in the student name and ID, then name the final archive GD_Hw1_[firstName]_[lastName]_[StudentId].zip. No remote URL or personal information has been invented.',y)
    y=paragraph(c,'<b>Credits.</b> Implementation, pixel artwork, synthesized sound effects, and documentation were prepared with OpenAI Codex assistance. The head was generated from the user-provided facial reference with the built-in image-generation tool. The final prompt and asset source are in Docs/ArtDirection.md. Liberation Sans, TextMeshPro, and Unity packages retain their supplied licenses. Review and acknowledge assistance according to course rules.',y)
    c.save()
    print(output)

if __name__=='__main__':main()
