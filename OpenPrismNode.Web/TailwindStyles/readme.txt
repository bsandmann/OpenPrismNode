A few words on how tailwind works here:

The installation of tailwind was done according to this video:
https://www.youtube.com/watch?v=QIdedo8iI4Y&ab_channel=dotnet

TLDR;
Just run this from the terminal:
npx tailwindcss -i .\TailwindStyles\app.css -o .\wwwroot\app.css --watch
On linux:
npx tailwindcss -i ./TailwindStyles/app.css -o ./wwwroot/app.css --watch

-- 
The longer explanation:
1) First when setting up tailwind, it has to be installed
npx tailwindcss init
I'm using nvm / node 20.11.0. That worked fine
This command creates a tailwind.config.js file in the root of the project

2) In that configuration file it generated, I added the following:
module.exports = {
  content: ['./**/*.{razor,html}'],
  theme: {
    extend: {},
  },
  plugins: [],
}
This basically tells tailwind to look for razor and html files in the project
for the tailwind specific attributes

3) run the command from the tldr:
npx tailwindcss -i .\TailwindStyles\app.css -o .\wwwroot\app.css --watch
This looks into the TailwindStyles folder and generates a css file in the wwwroot folder
based on that input.
Of course the app.css has to be linked in the App.razor file

4) Lastly I don't only want this build to happen when I run the command, but also when I build the project.
So we need to add a pre-build event to the project.

5) To have auto completion for tailwind, we additionally have to install
with "npm install". We acutally don't use the node-folder or anything
else here, but only this way tailwind seems to be recognized by the IDE
All full introduction can be found here:
https://www.jetbrains.com/help/rider/Tailwind_CSS.html#ws_css_tailwind_install
But I'm not following this exactly, but only the npm install part