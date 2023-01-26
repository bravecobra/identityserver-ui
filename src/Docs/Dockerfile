FROM python:3.11.1
RUN pip install mkdocs
RUN pip install mkdocs-material
COPY ./docs /app/docs
COPY ./mkdocs.yml /app/
WORKDIR /app/
EXPOSE 8080
CMD ["mkdocs", "serve"]