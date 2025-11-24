Загрузчик из TFS указанных проектов

Для разработки стоит создавать appsettings.Development.json и input.Development.json
Если просто применять - то хватит и  appsettings.json и input.json

Пример appsettings.json:
```
{
      "TfsToken":"vodoqdxxqxx3uinypssdd7asdnqdfuqy77cy27fq", <-- токен доступа к tfs
      "TfsUri":"https://tfs:8443/tfs",<-- адрес tfs
      "RootFolder":"D:\\_Outputs\\TestTfs", <-- папка для хранения резульатов
      "ProjectNames":["Platform", "External"], <-- имена проектов откуда забирать. Проекты которые не указаны в этом списке будут проигнорированы
      "DeleteTests":true <-- удалять ли тесты из проекта. Удаление по маске  "*Test*.sln" и "*Test*.csproj" 
}
```

input.json брался из subscription.json в проектах. Поэтому часть полей не рабочие (возможно в будущем задействуются)

Пример input.json
```
{
  "enabled": true, <-- не влияет ни на что
  "action": "sync",<-- не влияет ни на что
  "units": [
    {
      "collection": "Collection", <-- коллекция
      "project": "Platform", <-- проект в коллекции
      "repositoryName" : "host", <-- репозиторий
      "definition": {
        "name": "host" <-- имя пайплайна. Зачастую совпадает с именем репозитория, но не всегда
      },
      "branch": "prod", <-- ветка
      "robocopy": []<-- не влияет ни на что
    },
    {
      "collection": "Collection", <-- коллекция
      "project": "Platform", <-- проект в коллекции
      "definition": {
        "name": "thirdparty" <-- имя пайплайна. Зачастую совпадает с именем репозитория, но не всегда
      },
      "branch": "master", <-- ветка
      "robocopy": []<-- не влияет ни на что
    }
  ]
}
```