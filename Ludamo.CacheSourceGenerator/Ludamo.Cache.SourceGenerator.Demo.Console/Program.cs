// See https://aka.ms/new-console-template for more information
using Ludamo.Cache.SourceGenerator.Demo.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

var serviceCollection = new ServiceCollection();

serviceCollection.AddMemoryCache();
//serviceCollection.AddTransient<IUserService, UserService>();
serviceCollection.AddUserServiceCacheDecorator();


var serviceProvider = serviceCollection.BuildServiceProvider();

// var usersJson = await userService.GetUsersAsync();

// var decorated = serviceProvider.GetRequiredService<UserServiceCacheDecorator>();
var decorated = serviceProvider.GetRequiredService<IUserService>();

var usersJson = await decorated.GetUsersAsync();

Console.WriteLine(usersJson);

Console.ReadLine();

var user = await decorated.GetUserAsync(1);

Console.WriteLine(user);

Console.ReadLine();

user = await decorated.GetUserAsync(1);

Console.WriteLine(user);



