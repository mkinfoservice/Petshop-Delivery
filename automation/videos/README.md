# Vídeos gravados

Os vídeos são salvos automaticamente pelo Playwright em:

```
E:\videos-vendapps\
```

Cada execução de teste gera uma subpasta:

```
E:\videos-vendapps\
├── 01-cadastrar-cliente-tutoriais\
│   └── video.webm
├── 02-editar-cliente-tutoriais\
│   └── video.webm
└── ...
```

## Esta pasta no repositório

Esta pasta (`automation/videos/`) é um marcador de estrutura no repositório.
Os arquivos `.webm` e `.mp4` **não são commitados** (ver `.gitignore`).

## Converter webm → mp4

```bash
ffmpeg -i "E:\videos-vendapps\01-cadastrar-cliente-tutoriais\video.webm" ^
       -c:v libx264 -c:a aac ^
       "E:\videos-vendapps\01-cadastrar-cliente.mp4"
```
